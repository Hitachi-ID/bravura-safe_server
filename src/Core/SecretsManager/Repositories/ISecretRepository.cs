﻿using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;

namespace Bit.Core.SecretsManager.Repositories;

public interface ISecretRepository
{
    Task<IEnumerable<Secret>> GetManyByOrganizationIdAsync(Guid organizationId, Guid userId, AccessClientType accessType);
    Task<IEnumerable<Secret>> GetManyByOrganizationIdInTrashAsync(Guid organizationId);
    Task<IEnumerable<Secret>> GetManyByOrganizationIdInTrashByIdsAsync(Guid organizationId, IEnumerable<Guid> ids);
    Task<IEnumerable<Secret>> GetManyByIds(IEnumerable<Guid> ids);
    Task<IEnumerable<Secret>> GetManyByProjectIdAsync(Guid projectId, Guid userId, AccessClientType accessType);
    Task<Secret> GetByIdAsync(Guid id);
    Task<Secret> CreateAsync(Secret secret);
    Task<Secret> UpdateAsync(Secret secret);
    Task SoftDeleteManyByIdAsync(IEnumerable<Guid> ids);
    Task HardDeleteManyByIdAsync(IEnumerable<Guid> ids);
    Task RestoreManyByIdAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<Secret>> ImportAsync(IEnumerable<Secret> secrets);
    Task UpdateRevisionDates(IEnumerable<Guid> ids);
}
