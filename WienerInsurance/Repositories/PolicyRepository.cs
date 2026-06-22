using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WienerInsurance.Models;

namespace WienerInsurance.Repositories
{
    public class PolicyRepository
    {
        private readonly string _connectionString;

        public PolicyRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        public async Task<bool> CreatePolicyAsync(Policy policy)
        {
            var query = @"
                    INSERT INTO Policies (PartnerId, PolicyNumber, Amount, CreatedAtUtc, CreatedByUserId)
                    VALUES (@PartnerId, @PolicyNumber, @Amount, @CreatedAtUtc, @CreatedByUserId)";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, policy);
            return rows > 0;
        }

        // isActive: true = only active, false = only inactive, null = all
        public async Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool? isActive = true)
        {
            var query = @"
                    SELECT p.*, (part.FirstName + ' ' + part.LastName) AS PartnerFullName 
                    FROM Policies p 
                    LEFT JOIN Partners part ON p.PartnerId = part.Id";

            object param = null;
            if (isActive.HasValue)
            {
                query += " WHERE ISNULL(p.IsActive, 1) = @IsActive";
                param = new { IsActive = isActive.Value ? 1 : 0 };
            }

            using var conn = Connection;
            return await conn.QueryAsync<Policy>(query, param);
        }      

        public async Task<bool> SoftDeletePolicyAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Policies SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

        public async Task<bool> RestorePolicyAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Policies SET IsActive = 1, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }
    }
}
