﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Entities.Provider;
using Bit.Core.Models.Business;
using Bit.Core.Services;
using Bit.Core.Settings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Services
{
    public class HandlebarsMailServiceTests
    {
        private readonly HandlebarsMailService _sut;

        private readonly GlobalSettings _globalSettings;
        private readonly IMailDeliveryService _mailDeliveryService;
        private readonly IMailEnqueuingService _mailEnqueuingService;

        public HandlebarsMailServiceTests()
        {
            _globalSettings = new GlobalSettings();
            _mailDeliveryService = Substitute.For<IMailDeliveryService>();
            _mailEnqueuingService = Substitute.For<IMailEnqueuingService>();

            _sut = new HandlebarsMailService(
                _globalSettings,
                _mailDeliveryService,
                _mailEnqueuingService
            );
        }

        [Fact(Skip = "For local development")]
        public async Task SendAllEmails()
        {
            // This test is only opt in and is more for development purposes.
            // This will send all emails to the test email address so that they can be viewed.
            var namedParameters = new Dictionary<(string, Type), object>
            {
                // TODO: Swith to use env variable
                { ("email", typeof(string)), "test@safe.hitachi-id.net" },
                { ("user", typeof(User)), new User
                {
                    Id = Guid.NewGuid(),
                    Email = "test@safe.hitachi-id.net",
                }},
                { ("userId", typeof(Guid)), Guid.NewGuid() },
                { ("token", typeof(string)), "test_token" },
                { ("fromEmail", typeof(string)), "test@safe.hitachi-id.net" },
                { ("toEmail", typeof(string)), "test@safe.hitachi-id.net" },
                { ("newEmailAddress", typeof(string)), "test@safe.hitachi-id.net" },
                { ("hint", typeof(string)), "Test Hint" },
                { ("organizationName", typeof(string)), "Test Team Name" },
                { ("orgUser", typeof(OrganizationUser)), new OrganizationUser
                {
                    Id = Guid.NewGuid(),
                    Email = "test@safe.hitachi-id.net",
                    OrganizationId = Guid.NewGuid(),

                }},
                { ("token", typeof(ExpiringToken)), new ExpiringToken("test_token", DateTime.UtcNow.AddDays(1))},
                { ("organization", typeof(Organization)), new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Team Name",
                    Seats = 5
                }},
                { ("initialSeatCount", typeof(int)), 5},
                { ("ownerEmails", typeof(IEnumerable<string>)), new [] { "test@safe.hitachi-id.net" }},
                { ("maxSeatCount", typeof(int)), 5 },
                { ("userIdentifier", typeof(string)), "test_user" },
                { ("adminEmails", typeof(IEnumerable<string>)), new [] { "test@safe.hitachi-id.net" }},
                { ("returnUrl", typeof(string)), "https://safe.hitachi-id.net/" },
                { ("amount", typeof(decimal)), 1.00M },
                { ("dueDate", typeof(DateTime)), DateTime.UtcNow.AddDays(1) },
                { ("items", typeof(List<string>)), new List<string> { "test@safe.hitachi-id.net" }},
                { ("mentionInvoices", typeof(bool)), true },
                { ("emails", typeof(IEnumerable<string>)), new [] { "test@safe.hitachi-id.net" }},
                { ("deviceType", typeof(string)), "Mobile" },
                { ("timestamp", typeof(DateTime)), DateTime.UtcNow.AddDays(1)},
                { ("ip", typeof(string)), "127.0.0.1" },
                { ("emergencyAccess", typeof(EmergencyAccess)), new EmergencyAccess
                {
                    Id = Guid.NewGuid(),
                    Email = "test@safe.hitachi-id.net",
                }},
                { ("granteeEmail", typeof(string)), "test@safe.hitachi-id.net" },
                { ("grantorName", typeof(string)), "Test User" },
                { ("initiatingName", typeof(string)), "Test" },
                { ("approvingName", typeof(string)), "Test Name" },
                { ("rejectingName", typeof(string)), "Test Name" },
                { ("provider", typeof(Provider)), new Provider
                {
                    Id = Guid.NewGuid(),
                }},
                { ("name", typeof(string)), "Test Name" },
                { ("ea", typeof(EmergencyAccess)), new EmergencyAccess
                {
                    Id = Guid.NewGuid(),
                    Email = "test@safe.hitachi-id.net",
                }},
                { ("userName", typeof(string)), "testUser" },
                { ("orgName", typeof(string)), "Test Org Name" },
                { ("providerName", typeof(string)), "testProvider" },
                { ("providerUser", typeof(ProviderUser)), new ProviderUser
                {
                    ProviderId = Guid.NewGuid(),
                    Id = Guid.NewGuid(),
                }},
                { ("familyUserEmail", typeof(string)), "test@safe.hitachi-id.net" },
                { ("sponsorEmail", typeof(string)), "test@safe.hitachi-id.net" },
                { ("familyOrgName", typeof(string)), "Test Org Name" },
                { ("existingAccount", typeof(bool)), true },
                { ("sponsorshipEndDate", typeof(DateTime)), DateTime.UtcNow.AddDays(1)},
                { ("sponsorOrgName", typeof(string)), "Sponsor Test Org Name" },
                { ("expirationDate", typeof(DateTime)), DateTime.Now.AddDays(3) },
                { ("utcNow", typeof(DateTime)), DateTime.UtcNow },
            };

            var globalSettings = new GlobalSettings
            {
                Mail = new GlobalSettings.MailSettings
                {
                    Smtp = new GlobalSettings.MailSettings.SmtpSettings
                    {
                        Host = "localhost",
                        TrustServer = true,
                        Port = 10250,
                    },
                    ReplyToEmail = "noreply@safe.hitachi-id.net",
                },
                SiteName = "Bitwarden",
            };

            var mailDeliveryService = new MailKitSmtpMailDeliveryService(globalSettings, Substitute.For<ILogger<MailKitSmtpMailDeliveryService>>());

            var handlebarsService = new HandlebarsMailService(globalSettings, mailDeliveryService, new BlockingMailEnqueuingService());

            var sendMethods = typeof(IMailService).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Send") && m.Name != "SendEnqueuedMailMessageAsync");

            foreach (var sendMethod in sendMethods)
            {
                await InvokeMethod(sendMethod);
            }

            async Task InvokeMethod(MethodInfo method)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!namedParameters.TryGetValue((parameters[i].Name, parameters[i].ParameterType), out var value))
                    {
                        throw new InvalidOperationException($"Couldn't find a parameter for name '{parameters[i].Name}' and type '{parameters[i].ParameterType.FullName}'");
                    }

                    args[i] = value;
                }

                await (Task)method.Invoke(handlebarsService, args);
            }
        }

        // Remove this test when we add actual tests. It only proves that
        // we've properly constructed the system under test.
        [Fact]
        public void ServiceExists()
        {
            Assert.NotNull(_sut);
        }
    }
}
