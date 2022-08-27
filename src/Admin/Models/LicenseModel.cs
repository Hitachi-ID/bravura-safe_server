using System.ComponentModel.DataAnnotations;

namespace Bit.Admin.Models
{
    public class LicenseModel : IValidatableObject
    {
        [Display(Name = "User Id")]
        public Guid? UserId { get; set; }
        [Display(Name = "Team Id")]
        public Guid? OrganizationId { get; set; }
        [Display(Name = "Installation Id")]
        public Guid? InstallationId { get; set; }
        [Required]
        [Display(Name = "Version")]
        public int Version { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UserId.HasValue && OrganizationId.HasValue)
            {
                yield return new ValidationResult("Use either User Id or Team Id. Not both.");
            }

            if (!UserId.HasValue && !OrganizationId.HasValue)
            {
                yield return new ValidationResult("User Id or Team Id is required.");
            }

            if (OrganizationId.HasValue && !InstallationId.HasValue)
            {
                yield return new ValidationResult("Installation Id is required for team licenses.");
            }
        }
    }
}
