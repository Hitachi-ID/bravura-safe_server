using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models;
using Bit.Core.Settings;
using Bit.Core.Utilities.Hypr;

namespace Bit.Core.Identity
{
    public interface IOrganizationHyprWebTokenProvider : IOrganizationTwoFactorTokenProvider { }

    public class OrganizationHyprWebTokenProvider : IOrganizationHyprWebTokenProvider
    {
        private readonly GlobalSettings _globalSettings;

        public OrganizationHyprWebTokenProvider(GlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(Organization organization)
        {
            if (organization == null || !organization.Enabled || !organization.Use2fa)
            {
                return Task.FromResult(false);
            }

            var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
            var canGenerate = organization.TwoFactorProviderIsEnabled(TwoFactorProviderType.OrganizationHypr)
                && HasProperMetaData(provider);
            return Task.FromResult(canGenerate);
        }

        public Task<string> GenerateAsync(Organization organization, User user)
        {
            if (organization == null || !organization.Enabled || !organization.Use2fa)
            {
                return Task.FromResult<string>(null);
            }

            var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
            if (!HasProperMetaData(provider))
            {
                return Task.FromResult<string>(null);
            }

            var signatureRequest = HyprWeb.SignTxRequest(_globalSettings.Hypr.SKey, user.Email, organization.Id.ToString());
            return Task.FromResult(signatureRequest);
        }

        public Task<bool> ValidateAsync(string token, Organization organization, User user)
        {
            if (organization == null || !organization.Enabled || !organization.Use2fa)
            {
                return Task.FromResult(false);
            }

            var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
            if (!HasProperMetaData(provider))
            {
                return Task.FromResult(false);
            }

            var response = HyprWeb.VerifyAuthResponse(_globalSettings.Hypr.SKey, token);

            return Task.FromResult(response == user.Email);
        }

        private bool HasProperMetaData(TwoFactorProvider provider)
        {
            return provider?.MetaData != null && provider.MetaData.ContainsKey("AKey") &&
                provider.MetaData.ContainsKey("App") && provider.MetaData.ContainsKey("Server");
        }
    }
}
