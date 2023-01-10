﻿using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.OrganizationFeatures.OrganizationApiKeys;
using Bit.Core.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.OrganizationFeatures.OrganizationApiKeys;

[SutProviderCustomize]
public class GetOrganizationApiKeyCommandTests
{
    [Theory]
    [BitAutoData]
    public async Task GetOrganizationApiKey_HasOne_Returns(SutProvider<GetOrganizationApiKeyCommand> sutProvider,
        Guid id, Guid organizationId, OrganizationApiKeyType keyType)
    {
        sutProvider.GetDependency<IOrganizationApiKeyRepository>()
            .GetManyByOrganizationIdTypeAsync(organizationId, keyType)
            .Returns(new List<OrganizationApiKey>
            {
                new OrganizationApiKey
                {
                    Id = id,
                    OrganizationId = organizationId,
                    ApiKey = "test",
                    Type = keyType,
                    RevisionDate = DateTime.Now.AddDays(-1),
                },
            });

        var apiKey = await sutProvider.Sut.GetOrganizationApiKeyAsync(organizationId, keyType);
        Assert.NotNull(apiKey);
        Assert.Equal(id, apiKey.Id);
    }

    [Theory]
    [BitAutoData]
    public async Task GetOrganizationApiKey_HasTwo_Throws(SutProvider<GetOrganizationApiKeyCommand> sutProvider,
        Guid organizationId, OrganizationApiKeyType keyType)
    {
        sutProvider.GetDependency<IOrganizationApiKeyRepository>()
            .GetManyByOrganizationIdTypeAsync(organizationId, keyType)
            .Returns(new List<OrganizationApiKey>
            {
                new OrganizationApiKey
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    ApiKey = "test",
                    Type = keyType,
                    RevisionDate = DateTime.Now.AddDays(-1),
                },
                new OrganizationApiKey
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    ApiKey = "test_other",
                    Type = keyType,
                    RevisionDate = DateTime.Now.AddDays(-1),
                },
            });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sutProvider.Sut.GetOrganizationApiKeyAsync(organizationId, keyType));
    }

    [Theory]
    [BitAutoData]
    public async Task GetOrganizationApiKey_HasNone_CreatesAndReturns(SutProvider<GetOrganizationApiKeyCommand> sutProvider,
        Guid organizationId, OrganizationApiKeyType keyType)
    {
        sutProvider.GetDependency<IOrganizationApiKeyRepository>()
            .GetManyByOrganizationIdTypeAsync(organizationId, keyType)
            .Returns(Enumerable.Empty<OrganizationApiKey>());

        var apiKey = await sutProvider.Sut.GetOrganizationApiKeyAsync(organizationId, keyType);

        Assert.NotNull(apiKey);
        Assert.Equal(organizationId, apiKey.OrganizationId);
        Assert.Equal(keyType, apiKey.Type);
        await sutProvider.GetDependency<IOrganizationApiKeyRepository>()
            .Received(1)
            .CreateAsync(Arg.Any<OrganizationApiKey>());
    }

    [Theory]
    [BitAutoData]
    public async Task GetOrganizationApiKey_BadType_Throws(SutProvider<GetOrganizationApiKeyCommand> sutProvider,
        Guid organizationId, OrganizationApiKeyType keyType)
    {
        keyType = (OrganizationApiKeyType)byte.MaxValue;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await sutProvider.Sut.GetOrganizationApiKeyAsync(organizationId, keyType));
    }
}
