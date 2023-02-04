namespace Bit.Core.Models.Api.Error.Hypr
{
    public class HyprErrorResponseJson
    {
        public string type { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public string detail { get; set; }
        public int errorCode { get; set; }
    }
}
