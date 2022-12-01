using System.ComponentModel.DataAnnotations;
namespace Bit.Api.Models.Request.Hypr
{
    public class HyprAuthenticationRequestModel
    {
        [Required]
        public string Signature { get; set; }
        public string Team { get; set; }
        public bool MobileBrowser { get; set; }

        public HyprAuthenticationRequestModel()
        {
            MobileBrowser = false;
        }
    }

    public class HyprAuthRequestJson
    {
        public string actionId { get; set; }
        public string appId { get; set; }
        public string machineId { get; set; }
        public string namedUser { get; set; }
        public string machine { get; set; }
        public string sessionNonce { get; set; }
        public string deviceNonce { get; set; }
        public string serviceNonce { get; set; }
        public string serviceHmac { get; set; }
        public HyprAuthRequestJsonAdditionalDetails additionalDetails { get; set; }
    }

    public class HyprAuthRequestJsonAdditionalDetails
    {
        public bool mobileBrowser { get; set; }
    }
}
