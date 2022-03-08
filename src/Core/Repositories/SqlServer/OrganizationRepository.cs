using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Bit.Core.Settings;
using Dapper;

namespace Bit.Core.Repositories.SqlServer
{
    public class OrganizationRepository : Repository<Organization, Guid>, IOrganizationRepository
    {
        private byte[] cryptKey;
        private byte[] authKey;

        public OrganizationRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
        {
            cryptKey = Convert.FromBase64String(globalSettings.SqlServer.CryptKey);
            authKey = Convert.FromBase64String(globalSettings.SqlServer.AuthKey);
        }

        public OrganizationRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
        { }

        public async Task<Organization> GetByIdentifierAsync(string identifier)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_ReadByIdentifier]",
                    new { Identifier = identifier },
                    commandType: CommandType.StoredProcedure);

                var result = results.SingleOrDefault();
                return result?.Decrypt(cryptKey, authKey);
            }
        }

        public async Task<ICollection<Organization>> GetManyByEnabledAsync()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_ReadByEnabled]",
                    commandType: CommandType.StoredProcedure);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }

        public async Task<ICollection<Organization>> GetManyByUserIdAsync(Guid userId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_ReadByUserId]",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }

        public async Task<ICollection<Organization>> SearchAsync(string name, string userEmail, bool? paid,
            int skip, int take)
        {
            using (var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<Organization>(
                    "[dbo].[Organization_Search]",
                    new { Name = name, UserEmail = userEmail, Paid = paid, Skip = skip, Take = take },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }

        public async Task UpdateStorageAsync(Guid id)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    "[dbo].[Organization_UpdateStorage]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180);
            }
        }

        public async Task<ICollection<OrganizationAbility>> GetManyAbilitiesAsync()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<OrganizationAbility>(
                    "[dbo].[Organization_ReadAbilities]",
                    commandType: CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public override async Task<Organization> GetByIdAsync(Guid id)
        {
            return (await base.GetByIdAsync(id))?.Decrypt(cryptKey, authKey);
        }

        public override async Task<Organization> CreateAsync(Organization org)
        {
            org.Encrypt(cryptKey, authKey);
            return (await base.CreateAsync(org))?.Decrypt(cryptKey, authKey);
        }

        public override async Task ReplaceAsync(Organization org)
        {
            org.Encrypt(cryptKey, authKey);
            await base.ReplaceAsync(org);
            org.Decrypt(cryptKey, authKey);
        }
    }
}
