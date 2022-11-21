using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.Api.Models.Response.Hypr
{
    public class HyprAuthenticationResponseModel
    {
        [Required]
        public int status { get; set; }
        public string? signature { get; set; }
    }

    public class HyprAuthResponseJson
    {
        public HyprAuthResponseJsonStatus status { get; set; }
        public HyprAuthResponseJsonResponse response { get; set; }
    }

    public class HyprAuthResponseJsonStatus
    {
        public int responseCode { get; set; }
        public string responeMessage { get; set; }
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
}
