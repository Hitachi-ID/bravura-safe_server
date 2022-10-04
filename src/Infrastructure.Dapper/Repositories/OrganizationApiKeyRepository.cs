using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Dapper;

namespace Bit.Infrastructure.Dapper.Repositories
{
    public class OrganizationApiKeyRepository : Repository<OrganizationApiKey, Guid>, IOrganizationApiKeyRepository
    {
        private byte[] cryptKey;
        private byte[] authKey;

        public OrganizationApiKeyRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
        {
            cryptKey = Convert.FromBase64String(globalSettings.SqlServer.CryptKey);
            authKey = Convert.FromBase64String(globalSettings.SqlServer.AuthKey);
        }

        public OrganizationApiKeyRepository(string connectionString, string readOnlyConnectionString)
            : base(connectionString, readOnlyConnectionString)
        { }

        public async Task<IEnumerable<OrganizationApiKey>> GetManyByOrganizationIdTypeAsync(Guid organizationId, OrganizationApiKeyType? type = null)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<OrganizationApiKey>(
                    "[dbo].[OrganizationApikey_ReadManyByOrganizationIdType]",
                    new
                    {
                        OrganizationId = organizationId,
                        Type = type,
                    },
                    commandType: CommandType.StoredProcedure);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }

        public override async Task<OrganizationApiKey> GetByIdAsync(Guid id)
        {
            return (await base.GetByIdAsync(id))?.Decrypt(cryptKey, authKey);
        }

        public override async Task<OrganizationApiKey> CreateAsync(OrganizationApiKey org)
        {
            org.Encrypt(cryptKey, authKey);
            return (await base.CreateAsync(org))?.Decrypt(cryptKey, authKey);
        }

        public override async Task ReplaceAsync(OrganizationApiKey org)
        {
            org.Encrypt(cryptKey, authKey);
            await base.ReplaceAsync(org);
            org.Decrypt(cryptKey, authKey);
        }
    }
}
