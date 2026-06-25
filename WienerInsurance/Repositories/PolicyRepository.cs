using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WienerInsurance.Models;

namespace WienerInsurance.Repositories
{
    /// <summary>
    /// Repository for managing policy data persistence operations.
    /// </summary>
    public class PolicyRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the PolicyRepository class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public PolicyRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        /// <summary>
        /// Creates a new policy in the database.
        /// </summary>
        /// <param name="policy">The policy entity to create.</param>
        /// <returns>True if the policy was successfully created; otherwise, false.</returns>
        public async Task<bool> CreatePolicyAsync(Policy policy)
        {
            var query = @"
                    INSERT INTO Policies (PartnerId, PolicyNumber, Amount, CreatedAtUtc, CreatedByUserId)
                    VALUES (@PartnerId, @PolicyNumber, @Amount, @CreatedAtUtc, @CreatedByUserId)";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, policy);
            return rows > 0;
        }

        /// <summary>
        /// Retrieves all policies from the database with optional filtering.
        /// </summary>
        /// <param name="isActive">Filter by active status. True returns only active policies, false returns only inactive, null returns all.</param>
        /// <param name="partnerId">Filter by partner ID.</param>
        /// <param name="searchPolicyNumber">Search by policy number (partial match).</param>
        /// <returns>A collection of policies matching the filter criteria.</returns>
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
            var result = await conn.QueryAsync<Policy>(query, param);
            return result ?? Enumerable.Empty<Policy>();
        }

        /// <summary>
        /// Retrieves a paginated list of policies with optional filtering.
        /// </summary>
        /// <param name="isActive">Filter by active status. True returns only active policies, false returns only inactive, null returns all.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="partnerId">Filter by partner ID.</param>
        /// <param name="searchPolicyNumber">Search by policy number (partial match).</param>
        /// <returns>A tuple containing the paginated list of policies and the total count of matching records.</returns>
        public async Task<(IEnumerable<Policy> items, int totalCount)> GetAllPoliciesPaginatedAsync(bool? isActive = true, int pageNumber = 1, int pageSize = 10, int? partnerId = null, string searchPolicyNumber = null)
        {
            var whereConditions = new List<string>();
            var param = new DynamicParameters();
            param.Add("@PageSize", pageSize);
            param.Add("@PageNumber", pageNumber);

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

            var whereClause = whereConditions.Count > 0 ? " WHERE " + string.Join(" AND ", whereConditions) : "";

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
            return (items ?? Enumerable.Empty<Policy>(), totalCount);
        }

        /// <summary>
        /// Performs a soft delete on a policy by marking it as inactive.
        /// </summary>
        /// <param name="id">The unique identifier of the policy to delete.</param>
        /// <param name="modifiedAtUtc">The UTC timestamp when the deletion occurred.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the deletion.</param>
        /// <returns>True if the policy was successfully marked as inactive; otherwise, false.</returns>
        public async Task<bool> SoftDeletePolicyAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Policies SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

        /// <summary>
        /// Restores a soft-deleted policy by marking it as active.
        /// </summary>
        /// <param name="id">The unique identifier of the policy to restore.</param>
        /// <param name="modifiedAtUtc">The UTC timestamp when the restoration occurred.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the restoration.</param>
        /// <returns>True if the policy was successfully restored; otherwise, false.</returns>
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
