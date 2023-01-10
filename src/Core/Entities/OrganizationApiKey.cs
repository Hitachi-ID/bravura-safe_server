using System.ComponentModel.DataAnnotations;
using Bit.Core.Enums;
using Bit.Core.Utilities;
using Bit.Core.Utilities.Crypto;

namespace Bit.Core.Entities;

public class OrganizationApiKey : ITableObject<Guid>
{
    private bool? encrypted;
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationApiKeyType Type { get; set; }
    public string ApiKey { get; set; }
    public DateTime RevisionDate { get; set; }

    public void SetNewId()
    {
        Id = CoreHelpers.GenerateComb();
    }

    public OrganizationApiKey Encrypt(byte[] cryptKey, byte[] authKey)
    {
        if (encrypted == true)
            return this;
        if (ApiKey is not null) ApiKey = AESHMACEncryption.SimpleEncrypt(ApiKey, cryptKey, authKey);
        encrypted = true;
        return this;
    }

    public OrganizationApiKey Decrypt(byte[] cryptKey, byte[] authKey)
    {
        if (encrypted == false)
            return this;
        if (ApiKey is not null) ApiKey = AESHMACEncryption.SimpleDecrypt(ApiKey, cryptKey, authKey);
        encrypted = false;
        return this;
    }
}
