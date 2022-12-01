using System;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models;
using Bit.Core.Models.Api;

namespace Bit.Api.Models.Response.TwoFactor
{
    public class TwoFactorHyprResponseModel : ResponseModel
    {
        private const string ResponseObj = "twoFactorHypr";

        public TwoFactorHyprResponseModel(Organization org)
            : base(ResponseObj)
        {
            if (org == null)
            {
                throw new ArgumentNullException(nameof(org));
            }

            var provider = org.GetTwoFactorProvider(TwoFactorProviderType.OrganizationHypr);
            Build(provider);
        }

        public bool Enabled { get; set; }
        public string ServerURL { get; set; }
        public string ApiKey { get; set; }
        public string AppID { get; set; }

        private void Build(TwoFactorProvider provider)
        {
            if (provider?.MetaData != null && provider.MetaData.Count > 0)
            {
                Enabled = provider.Enabled;

                if (provider.MetaData.ContainsKey("Server"))
                {
                    ServerURL = (string)provider.MetaData["Server"];
                }
                if (provider.MetaData.ContainsKey("AKey"))
                {
                    ApiKey = (string)provider.MetaData["AKey"];
                }
                if (provider.MetaData.ContainsKey("App"))
                {
                    AppID = (string)provider.MetaData["App"];
                }
            }
            else
            {
                Enabled = false;
            }
        }
    }
}
