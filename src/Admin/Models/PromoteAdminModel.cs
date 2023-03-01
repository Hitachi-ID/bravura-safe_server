using System.ComponentModel.DataAnnotations;

namespace Bit.Admin.Models;

public class PromoteAdminModel
{
    [Required]
    [Display(Name = "Admin User Id")]
    public Guid? UserId { get; set; }
    [Required]
    [Display(Name = "Team Id")]
    public Guid? OrganizationId { get; set; }
}
