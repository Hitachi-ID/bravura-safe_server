using Bit.Core.Models.Table;
using Bit.Core.Enums;
using Bit.Core.Models.Business;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Bit.Core.Utilities;

namespace Bit.Core.Models.Api
{
    public class OrganizationCreateRequestModel : IValidatableObject
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(50)]
        public string BusinessName { get; set; }
        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string BillingEmail { get; set; }
        public PlanType PlanType { get; set; }
        [Required]
        public string Key { get; set; }
        public OrganizationKeysRequestModel Keys { get; set; }
        public PaymentMethodType? PaymentMethodType { get; set; }
        public string PaymentToken { get; set; }
        [Range(0, int.MaxValue)]
        public int AdditionalSeats { get; set; }
        [Range(0, 99)]
        public short? AdditionalStorageGb { get; set; }
        public bool PremiumAccessAddon { get; set; }
        [EncryptedString]
        [EncryptedStringLength(1000)]
        public string CollectionName { get; set; }
        public string TaxIdNumber { get; set; }
        public string BillingAddressLine1 { get; set; }
        public string BillingAddressLine2 { get; set; }
        public string BillingAddressCity { get; set; }
        public string BillingAddressState { get; set; }
        public string BillingAddressPostalCode { get; set; }
        [StringLength(2)]
        public string BillingAddressCountry { get; set; }

        public virtual OrganizationSignup ToOrganizationSignup(User user)
        {
            var orgSignup = new OrganizationSignup
            {
                Owner = user,
                OwnerKey = Key,
                Name = Name,
                Plan = PlanType,
                PaymentMethodType = PaymentMethodType,
                PaymentToken = PaymentToken,
                AdditionalSeats = AdditionalSeats,
                AdditionalStorageGb = AdditionalStorageGb.GetValueOrDefault(0),
                PremiumAccessAddon = PremiumAccessAddon,
                BillingEmail = BillingEmail,
                BusinessName = BusinessName,
                CollectionName = CollectionName,
                TaxInfo = new TaxInfo
                {
                    TaxIdNumber = TaxIdNumber,
                    BillingAddressLine1 = BillingAddressLine1,
                    BillingAddressLine2 = BillingAddressLine2,
                    BillingAddressCity = BillingAddressCity,
                    BillingAddressState = BillingAddressState,
                    BillingAddressPostalCode = BillingAddressPostalCode,
                    BillingAddressCountry = BillingAddressCountry,
                },
            };

            Keys?.ToOrganizationSignup(orgSignup);

            return orgSignup;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlanType == PlanType.Free)
            {
                yield return new ValidationResult("Plan type is free.", new string[] { nameof(PaymentToken) });
            }
        }
    }
}
