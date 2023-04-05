﻿using System.Net;
using System.Net.Http.Headers;
using Bit.Api.IntegrationTest.Factories;
using Bit.Api.IntegrationTest.SecretsManager.Enums;
using Bit.Api.Models.Response;
using Bit.Api.SecretsManager.Models.Request;
using Bit.Api.SecretsManager.Models.Response;
using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Repositories;
using Bit.Test.Common.Helpers;
using Xunit;

namespace Bit.Api.IntegrationTest.SecretsManager.Controllers;

public class SecretsControllerTest : IClassFixture<ApiApplicationFactory>, IAsyncLifetime
{
    private readonly string _mockEncryptedString =
        "2.3Uk+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98sp4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=";

    private readonly HttpClient _client;
    private readonly ApiApplicationFactory _factory;
    private readonly ISecretRepository _secretRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAccessPolicyRepository _accessPolicyRepository;

    private string _email = null!;
    private SecretsManagerOrganizationHelper _organizationHelper = null!;

    public SecretsControllerTest(ApiApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _secretRepository = _factory.GetService<ISecretRepository>();
        _projectRepository = _factory.GetService<IProjectRepository>();
        _accessPolicyRepository = _factory.GetService<IAccessPolicyRepository>();
    }

    public async Task InitializeAsync()
    {
        _email = $"integration-test{Guid.NewGuid()}@bitwarden.com";
        await _factory.LoginWithNewAccount(_email);
        _organizationHelper = new SecretsManagerOrganizationHelper(_factory, _email);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task LoginAsync(string email)
    {
        var tokens = await _factory.LoginAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Token);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ListByOrganization_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var response = await _client.GetAsync($"/organizations/{org.Id}/secrets");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task ListByOrganization_Success(PermissionType permissionType)
    {
        var (org, orgUserOwner) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project
        {
            Id = new Guid(),
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var secretIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var secret = await _secretRepository.CreateAsync(new Secret
            {
                OrganizationId = org.Id,
                Key = _mockEncryptedString,
                Value = _mockEncryptedString,
                Note = _mockEncryptedString,
                Projects = new List<Project> { project }

            });
            secretIds.Add(secret.Id);
        }

        var response = await _client.GetAsync($"/organizations/{org.Id}/secrets");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SecretWithProjectsListResponseModel>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Secrets);
        Assert.Equal(secretIds.Count, result.Secrets.Count());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Create_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var request = new SecretCreateRequestModel
        {
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString
        };

        var response = await _client.PostAsJsonAsync($"/organizations/{org.Id}/secrets", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWithoutProject_RunAsAdmin_Success()
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project { Name = "123" });

        var request = new SecretCreateRequestModel
        {
            ProjectIds = new Guid[] { project.Id },
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString,
        };

        var response = await _client.PostAsJsonAsync($"/organizations/{org.Id}/secrets", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SecretResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Key, result!.Key);
        Assert.Equal(request.Value, result.Value);
        Assert.Equal(request.Note, result.Note);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);

        var createdSecret = await _secretRepository.GetByIdAsync(new Guid(result.Id));
        Assert.NotNull(result);
        Assert.Equal(request.Key, createdSecret.Key);
        Assert.Equal(request.Value, createdSecret.Value);
        Assert.Equal(request.Note, createdSecret.Note);
        AssertHelper.AssertRecent(createdSecret.RevisionDate);
        AssertHelper.AssertRecent(createdSecret.CreationDate);
        Assert.Null(createdSecret.DeletedDate);
    }

    [Fact]
    public async Task CreateWithoutProject_RunAsUser_NotFound()
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var request = new SecretCreateRequestModel
        {
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString
        };

        var response = await _client.PostAsJsonAsync($"/organizations/{org.Id}/secrets", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateWithProject_Success(PermissionType permissionType)
    {
        var (org, orgAdminUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        AccessClientType accessType = AccessClientType.NoAccessCheck;

        var project = await _projectRepository.CreateAsync(new Project()
        {
            Id = new Guid(),
            OrganizationId = org.Id,
            Name = _mockEncryptedString
        });

        var orgUserId = (Guid)orgAdminUser.UserId;

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            accessType = AccessClientType.User;

            var accessPolicies = new List<BaseAccessPolicy>
            {
                new Core.SecretsManager.Entities.UserProjectAccessPolicy
                {
                    GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id , Read = true, Write = true,
                },
            };
            orgUserId = (Guid)orgUser.UserId;
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var secretRequest = new SecretCreateRequestModel()
        {
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString,
            ProjectIds = new[] { project.Id },
        };
        var secretResponse = await _client.PostAsJsonAsync($"/organizations/{org.Id}/secrets", secretRequest);
        secretResponse.EnsureSuccessStatusCode();
        var secretResult = await secretResponse.Content.ReadFromJsonAsync<SecretResponseModel>();

        var secret = (await _secretRepository.GetManyByProjectIdAsync(project.Id, orgUserId, accessType)).First();

        Assert.NotNull(secretResult);
        Assert.Equal(secret.Id.ToString(), secretResult!.Id);
        Assert.Equal(secret.OrganizationId.ToString(), secretResult.OrganizationId);
        Assert.Equal(secret.Key, secretResult.Key);
        Assert.Equal(secret.Value, secretResult.Value);
        Assert.Equal(secret.Note, secretResult.Note);
        Assert.Equal(secret.CreationDate, secretResult.CreationDate);
        Assert.Equal(secret.RevisionDate, secretResult.RevisionDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Get_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var secret = await _secretRepository.CreateAsync(new Secret
        {
            OrganizationId = org.Id,
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString
        });

        var response = await _client.GetAsync($"/organizations/secrets/{secret.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task Get_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project()
        {
            Id = new Guid(),
            OrganizationId = org.Id,
            Name = _mockEncryptedString
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }
        else
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.Admin, true);
            await LoginAsync(email);
        }

        var secret = await _secretRepository.CreateAsync(new Secret
        {
            OrganizationId = org.Id,
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString,
            Projects = new List<Project> { project }
        });

        var response = await _client.GetAsync($"/secrets/{secret.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SecretResponseModel>();
        Assert.Equal(secret.Key, result!.Key);
        Assert.Equal(secret.Value, result.Value);
        Assert.Equal(secret.Note, result.Note);
        Assert.Equal(secret.RevisionDate, result.RevisionDate);
        Assert.Equal(secret.CreationDate, result.CreationDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Update_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var secret = await _secretRepository.CreateAsync(new Secret
        {
            OrganizationId = org.Id,
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString
        });

        var request = new SecretUpdateRequestModel
        {
            Key = _mockEncryptedString,
            Value = "2.3Uk+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98xy4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=",
            Note = _mockEncryptedString
        };

        var response = await _client.PutAsJsonAsync($"/organizations/secrets/{secret.Id}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task Update_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project()
        {
            Id = new Guid(),
            OrganizationId = org.Id,
            Name = _mockEncryptedString
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var secret = await _secretRepository.CreateAsync(new Secret
        {
            OrganizationId = org.Id,
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString,
            Projects = permissionType == PermissionType.RunAsUserWithPermission ? new List<Project>() { project } : null
        });

        var request = new SecretUpdateRequestModel()
        {
            Key = _mockEncryptedString,
            Value = "2.3Uk+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98xy4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=",
            Note = _mockEncryptedString,
            ProjectIds = permissionType == PermissionType.RunAsUserWithPermission ? new Guid[] { project.Id } : null
        };

        var response = await _client.PutAsJsonAsync($"/secrets/{secret.Id}", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SecretResponseModel>();
        Assert.Equal(request.Key, result!.Key);
        Assert.Equal(request.Value, result.Value);
        Assert.NotEqual(secret.Value, result.Value);
        Assert.Equal(request.Note, result.Note);
        AssertHelper.AssertRecent(result.RevisionDate);
        Assert.NotEqual(secret.RevisionDate, result.RevisionDate);

        var updatedSecret = await _secretRepository.GetByIdAsync(new Guid(result.Id));
        Assert.NotNull(result);
        Assert.Equal(request.Key, updatedSecret.Key);
        Assert.Equal(request.Value, updatedSecret.Value);
        Assert.Equal(request.Note, updatedSecret.Note);
        AssertHelper.AssertRecent(updatedSecret.RevisionDate);
        AssertHelper.AssertRecent(updatedSecret.CreationDate);
        Assert.Null(updatedSecret.DeletedDate);
        Assert.NotEqual(secret.Value, updatedSecret.Value);
        Assert.NotEqual(secret.RevisionDate, updatedSecret.RevisionDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Delete_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var secret = await _secretRepository.CreateAsync(new Secret
        {
            OrganizationId = org.Id,
            Key = _mockEncryptedString,
            Value = _mockEncryptedString,
            Note = _mockEncryptedString
        });
        var secretIds = new[] { secret.Id };

        var response = await _client.PostAsJsonAsync($"/secrets/{org.Id}/delete", secretIds);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task Delete_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project()
        {
            Id = new Guid(),
            OrganizationId = org.Id,
            Name = _mockEncryptedString
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }


        var secretIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var secret = await _secretRepository.CreateAsync(new Secret
            {
                OrganizationId = org.Id,
                Key = _mockEncryptedString,
                Value = _mockEncryptedString,
                Note = _mockEncryptedString,
                Projects = new List<Project>() { project }
            });
            secretIds.Add(secret.Id);
        }

        var response = await _client.PostAsJsonAsync($"/secrets/delete", secretIds);
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<ListResponseModel<BulkDeleteResponseModel>>();
        Assert.NotNull(results);

        var index = 0;
        foreach (var result in results!.Data)
        {
            Assert.Equal(secretIds[index], result.Id);
            Assert.Null(result.Error);
            index++;
        }

        var secrets = await _secretRepository.GetManyByIds(secretIds);
        Assert.Empty(secrets);
    }
}
