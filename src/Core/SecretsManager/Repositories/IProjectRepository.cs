﻿using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;

namespace Bit.Core.SecretsManager.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetManyByOrganizationIdAsync(Guid organizationId, Guid userId, AccessClientType accessType);
    Task<IEnumerable<Project>> GetManyByIds(IEnumerable<Guid> ids);
    Task<Project> GetByIdAsync(Guid id);
    Task<Project> CreateAsync(Project project);
    Task ReplaceAsync(Project project);
    Task DeleteManyByIdAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<Project>> ImportAsync(IEnumerable<Project> projects);
    Task<bool> UserHasReadAccessToProject(Guid id, Guid userId);
    Task<bool> UserHasWriteAccessToProject(Guid id, Guid userId);
}
