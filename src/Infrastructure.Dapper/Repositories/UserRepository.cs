using System.Data;
using System.Data.SqlClient;
using Bit.Core.Entities;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Dapper;

namespace Bit.Infrastructure.Dapper.Repositories
{
    public class UserRepository : Repository<User, Guid>, IUserRepository
    {
        private byte[] cryptKey;
        private byte[] authKey;

        public UserRepository(GlobalSettings globalSettings)
            : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
        {
            cryptKey = Convert.FromBase64String(globalSettings.SqlServer.CryptKey);
            authKey = Convert.FromBase64String(globalSettings.SqlServer.AuthKey);
        }

        public UserRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
        { }

        public override async Task<User> GetByIdAsync(Guid id)
        {
            return (await base.GetByIdAsync(id))?.Decrypt(cryptKey, authKey);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_ReadByEmail]",
                    new { Email = email },
                    commandType: CommandType.StoredProcedure);

                var result = results.SingleOrDefault();
                result?.Decrypt(cryptKey, authKey);
                return result;
            }
        }

        public async Task<User> GetBySsoUserAsync(string externalId, Guid? organizationId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_ReadBySsoUserOrganizationIdExternalId]",
                    new { OrganizationId = organizationId, ExternalId = externalId },
                    commandType: CommandType.StoredProcedure);

                var result = results.SingleOrDefault();
                result?.Decrypt(cryptKey, authKey);
                return result;
            }
        }

        public async Task<UserKdfInformation> GetKdfInformationByEmailAsync(string email)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<UserKdfInformation>(
                    $"[{Schema}].[{Table}_ReadKdfByEmail]",
                    new { Email = email },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public async Task<ICollection<User>> SearchAsync(string email, int skip, int take)
        {
            using (var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_Search]",
                    new { Email = email, Skip = skip, Take = take },
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

        public async Task<ICollection<User>> GetManyByPremiumAsync(bool premium)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    "[dbo].[User_ReadByPremium]",
                    new { Premium = premium },
                    commandType: CommandType.StoredProcedure);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }

        public async Task<string> GetPublicKeyAsync(Guid id)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var results = await connection.QueryAsync<string>(
                    $"[{Schema}].[{Table}_ReadPublicKeyById]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public async Task<DateTime> GetAccountRevisionDateAsync(Guid id)
        {
            using (var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<DateTime>(
                    $"[{Schema}].[{Table}_ReadAccountRevisionDateById]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                return results.SingleOrDefault();
            }
        }

        public override async Task<User> CreateAsync(User user)
        {
            user.Encrypt(cryptKey, authKey);
            return (await base.CreateAsync(user))?.Decrypt(cryptKey, authKey);
        }

        public override async Task ReplaceAsync(User user)
        {
            user.Encrypt(cryptKey, authKey);
            await base.ReplaceAsync(user);
            user.Decrypt(cryptKey, authKey);
        }

        public override async Task DeleteAsync(User user)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    $"[{Schema}].[{Table}_DeleteById]",
                    new { Id = user.Id },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180);
            }
        }

        public async Task UpdateStorageAsync(Guid id)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    $"[{Schema}].[{Table}_UpdateStorage]",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180);
            }
        }

        public async Task UpdateRenewalReminderDateAsync(Guid id, DateTime renewalReminderDate)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    $"[{Schema}].[User_UpdateRenewalReminderDate]",
                    new { Id = id, RenewalReminderDate = renewalReminderDate },
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IEnumerable<User>> GetManyAsync(IEnumerable<Guid> ids)
        {
            using (var connection = new SqlConnection(ReadOnlyConnectionString))
            {
                var results = await connection.QueryAsync<User>(
                    $"[{Schema}].[{Table}_ReadByIds]",
                    new { Ids = ids.ToGuidIdArrayTVP() },
                    commandType: CommandType.StoredProcedure);

                var resultList = results.ToList();
                foreach (var result in resultList)
                {
                    result.Decrypt(cryptKey, authKey);
                }
                return resultList;
            }
        }
    }
}
