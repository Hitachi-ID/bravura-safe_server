﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Models.Api.Request.OrganizationSponsorships;
using Bit.Core.Models.Api.Response.OrganizationSponsorships;
using Bit.Core.Models.Data.Organizations.OrganizationSponsorships;
using Bit.Core.Models.OrganizationConnectionConfigs;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.SelfHosted
{
    public class SelfHostedSyncSponsorshipsCommand : BaseIdentityClientService, ISelfHostedSyncSponsorshipsCommand
    {
        private readonly IGlobalSettings _globalSettings;
        private readonly IOrganizationSponsorshipRepository _organizationSponsorshipRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly IOrganizationConnectionRepository _organizationConnectionRepository;

        public SelfHostedSyncSponsorshipsCommand(
        IHttpClientFactory httpFactory,
        IOrganizationSponsorshipRepository organizationSponsorshipRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationConnectionRepository organizationConnectionRepository,
        IGlobalSettings globalSettings,
        ILogger<SelfHostedSyncSponsorshipsCommand> logger)
        : base(
            httpFactory,
            globalSettings.Installation.ApiUri,
            globalSettings.Installation.IdentityUri,
            "api.installation",
            $"installation.{globalSettings.Installation.Id}",
            globalSettings.Installation.Key,
            logger)
        {
            _globalSettings = globalSettings;
            _organizationUserRepository = organizationUserRepository;
            _organizationSponsorshipRepository = organizationSponsorshipRepository;
            _organizationConnectionRepository = organizationConnectionRepository;
        }

        public async Task SyncOrganization(Guid organizationId, Guid cloudOrganizationId, OrganizationConnection billingSyncConnection)
        {
            if (!_globalSettings.EnableCloudCommunication)
            {
                throw new BadRequestException("Failed to sync instance with cloud - Cloud communication is disabled in global settings");
            }
            if (!billingSyncConnection.Enabled)
            {
                throw new BadRequestException($"Billing Sync Key disabled for team {organizationId}");
            }
            if (string.IsNullOrWhiteSpace(billingSyncConnection.Config))
            {
                throw new BadRequestException($"No Billing Sync Key known for team {organizationId}");
            }
            var billingSyncConfig = billingSyncConnection.GetConfig<BillingSyncConfig>();
            if (billingSyncConfig == null || string.IsNullOrWhiteSpace(billingSyncConfig.BillingSyncKey))
            {
                throw new BadRequestException($"Failed to get Billing Sync Key for team {organizationId}");
            }

            var organizationSponsorshipsDict = (await _organizationSponsorshipRepository.GetManyBySponsoringOrganizationAsync(organizationId))
                .ToDictionary(i => i.SponsoringOrganizationUserId);
            if (!organizationSponsorshipsDict.Any())
            {
                _logger.LogInformation($"No existing sponsorships to sync for team {organizationId}");
                return;
            }
            var syncedSponsorships = new List<OrganizationSponsorshipData>();

            foreach (var orgSponsorshipsBatch in CoreHelpers.Batch(organizationSponsorshipsDict.Values, 1000))
            {
                var response = await SendAsync<OrganizationSponsorshipSyncRequestModel, OrganizationSponsorshipSyncResponseModel>(HttpMethod.Post, "organization/sponsorship/sync", new OrganizationSponsorshipSyncRequestModel
                {
                    BillingSyncKey = billingSyncConfig.BillingSyncKey,
                    SponsoringOrganizationCloudId = cloudOrganizationId,
                    SponsorshipsBatch = orgSponsorshipsBatch.Select(s => new OrganizationSponsorshipRequestModel(s))
                });

                if (response == null)
                {
                    _logger.LogDebug("Team sync failed for '{OrgId}'", organizationId);
                    throw new BadRequestException("Team sync failed");
                }

                syncedSponsorships.AddRange(response.ToOrganizationSponsorshipSync().SponsorshipsBatch);
            }

            var sponsorshipsToDelete = syncedSponsorships.Where(s => s.CloudSponsorshipRemoved).Select(i => organizationSponsorshipsDict[i.SponsoringOrganizationUserId].Id);
            var sponsorshipsToUpsert = syncedSponsorships.Where(s => !s.CloudSponsorshipRemoved).Select(i =>
            {
                var existingSponsorship = organizationSponsorshipsDict[i.SponsoringOrganizationUserId];
                if (existingSponsorship != null)
                {
                    existingSponsorship.LastSyncDate = i.LastSyncDate;
                    existingSponsorship.ValidUntil = i.ValidUntil;
                    existingSponsorship.ToDelete = i.ToDelete;
                }
                else
                {
                    // shouldn't occur, added in case self hosted loses a sponsorship
                    existingSponsorship = new OrganizationSponsorship
                    {
                        SponsoringOrganizationId = organizationId,
                        SponsoringOrganizationUserId = i.SponsoringOrganizationUserId,
                        FriendlyName = i.FriendlyName,
                        OfferedToEmail = i.OfferedToEmail,
                        PlanSponsorshipType = i.PlanSponsorshipType,
                        LastSyncDate = i.LastSyncDate,
                        ValidUntil = i.ValidUntil,
                        ToDelete = i.ToDelete
                    };
                }
                return existingSponsorship;
            });

            if (sponsorshipsToDelete.Any())
            {
                await _organizationSponsorshipRepository.DeleteManyAsync(sponsorshipsToDelete);
            }
            if (sponsorshipsToUpsert.Any())
            {
                await _organizationSponsorshipRepository.UpsertManyAsync(sponsorshipsToUpsert);
            }
        }

    }
}