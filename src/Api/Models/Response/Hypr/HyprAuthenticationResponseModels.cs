using System.ComponentModel.DataAnnotations;

namespace Bit.Api.Models.Response.Hypr
{
    public class HyprAuthenticationResponseModel
    {
        [Required]
        public int status { get; set; }
        public string? signature { get; set; }
        public string? message { get; set; }
        public int? errorCode { get; set; }
    }

    public class HyprAuthResponseJson
    {
        public HyprAuthResponseJsonStatus status { get; set; }
        public HyprAuthResponseJsonResponse response { get; set; }
    }

    public class HyprAuthResponseJsonStatus
    {
        public int responseCode { get; set; }
        public string responseMessage { get; set; }
    }

    public class HyprAuthResponseJsonResponse
    {
        public string requestId { get; set; }
    }

    public class HyprAuthGetStatusResponseJson
    {
        public string requestId { get; set; }
        public string namedUser { get; set; }
        public string machine { get; set; }
        public HyprAuthGetStatusResponseJsonDevice device { get; set; }
        public IList<HyprAuthGetStatusResponseJsonState> state { get; set; }
    }

    public class HyprAuthGetStatusResponseJsonDevice
    {
        public string? deviceId { get; set; }
        public string? hmacDeviceKey { get; set; }
        public string? hmacSessionKey { get; set; }
    }

    public class HyprAuthGetStatusResponseJsonState
    {
        public string value { get; set; }
        public string message { get; set; }
        public int timestamp { get; set; }
    }

    public class HyprAuthGetMagicLink
    {
        public string url { get; set; }
        public string message { get; set; }
        public int status { get; set; }
    }

    public class HyprMagicLinkResponseModel
    {
        public string rpAppId { get; set; }
        public string username { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public string webLink { get; set; }
        public string mobileDeepLink { get; set; }
        public string message { get; set; }
        public string firebaseDynamicLinkForHyprApp { get; set; }
        public long createTimeUTC { get; set; }
        public long expirationTimeUTC { get; set; }
        public long? usedOnTimeUTC { get; set; }
        public string token { get; set; }
    }
}
