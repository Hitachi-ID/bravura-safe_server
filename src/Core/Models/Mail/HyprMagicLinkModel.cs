namespace Bit.Core.Models.Mail;

public class HyprMagicLinkModel : BaseMailModel
{
    public string Url { get; set; }
    public string TheDate { get; set; }
    public string TheTime { get; set; }
    public string TimeZone { get; set; }
}
