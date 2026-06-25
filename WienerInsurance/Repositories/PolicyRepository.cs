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
        public async Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool? isActive = true, int? partnerId = null, string searchPolicyNumber = null)
        {
            var whereConditions = new List<string>();
            var param = new DynamicParameters();

            if (isActive.HasValue)
            {
                whereConditions.Add("ISNULL(p.IsActive, 1) = @IsActive");
                param.Add("@IsActive", isActive.Value ? 1 : 0);
            }

            if (partnerId.HasValue)
            {
                whereConditions.Add("p.PartnerId = @PartnerId");
                param.Add("@PartnerId", partnerId.Value);
            }

            if (!string.IsNullOrEmpty(searchPolicyNumber))
            {
                whereConditions.Add("p.PolicyNumber LIKE @SearchPolicyNumber");
                param.Add("@SearchPolicyNumber", $"%{searchPolicyNumber}%");
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var query = $@"
                    SELECT p.*, CONCAT(part.FirstName, ' ', part.LastName) AS PartnerFullName 
                    FROM Policies p 
                    LEFT JOIN Partners part ON p.PartnerId = part.Id
                    {whereClause}
                    ORDER BY p.CreatedAtUtc DESC";

            using var conn = Connection;
            return await conn.QueryAsync<Policy>(query, param);
        }

        // isActive: true = only active, false = only inactive, null = all
        public async Task<(IEnumerable<Policy> items, int totalCount)> GetAllPoliciesPaginatedAsync(bool? isActive = true, int pageNumber = 1, int pageSize = 10)
        {
            var whereClause = "";
            var param = new DynamicParameters();
            param.Add("@PageSize", pageSize);
            param.Add("@PageNumber", pageNumber);

            if (isActive.HasValue)
            {
                whereClause = " WHERE ISNULL(p.IsActive, 1) = @IsActive";
                param.Add("@IsActive", isActive.Value ? 1 : 0);
            }

            // Get total count
            var countQuery = $"SELECT COUNT(*) FROM Policies p {whereClause}";
            using var conn = Connection;
            var totalCount = await conn.ExecuteScalarAsync<int>(countQuery, param);

            // Get paginated results
            var dataQuery = $@"
                    SELECT p.*, CONCAT(part.FirstName, ' ', part.LastName) AS PartnerFullName 
                    FROM Policies p 
                    LEFT JOIN Partners part ON p.PartnerId = part.Id
                    {whereClause}
                    ORDER BY p.CreatedAtUtc DESC
                    OFFSET (@PageNumber - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

            var items = await conn.QueryAsync<Policy>(dataQuery, param);
            return (items, totalCount);
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
