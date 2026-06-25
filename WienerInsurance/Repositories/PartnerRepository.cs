using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WienerInsurance.Models;

namespace WienerInsurance.Repositories
{
    /// <summary>
    /// Repository for managing partner data persistence operations.
    /// </summary>
    public class PartnerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the PartnerRepository class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public PartnerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        /// <summary>
        /// Retrieves all partners from the database with optional filtering.
        /// </summary>
        /// <param name="isActive">Filter by active status. Null returns all partners regardless of status.</param>
        /// <param name="partnerTypeId">Filter by partner type ID.</param>
        /// <param name="searchName">Search by first name or last name (partial match).</param>
        /// <param name="searchOib">Search by Croatian Personal Identification Number (partial match).</param>
        /// <param name="searchPartnerNumber">Search by partner number (partial match).</param>
        /// <returns>A collection of partners matching the filter criteria.</returns>
        public async Task<IEnumerable<Partner>> GetAllPartnersAsync(bool? isActive = null, int? partnerTypeId = null, string searchName = null, string searchOib = null, string searchPartnerNumber = null)
        {
            var whereConditions = new List<string>();
            var param = new DynamicParameters();

            if (isActive.HasValue)
            {
                whereConditions.Add("p.IsActive = @IsActive");
                param.Add("@IsActive", isActive.Value ? 1 : 0);
            }

            if (partnerTypeId.HasValue)
            {
                whereConditions.Add("p.PartnerTypeId = @PartnerTypeId");
                param.Add("@PartnerTypeId", partnerTypeId.Value);
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                whereConditions.Add("(p.FirstName LIKE @SearchName OR p.LastName LIKE @SearchName)");
                param.Add("@SearchName", $"%{searchName}%");
            }

            if (!string.IsNullOrEmpty(searchOib))
            {
                whereConditions.Add("p.CroatianPIN LIKE @SearchOib");
                param.Add("@SearchOib", $"%{searchOib}%");
            }

            if (!string.IsNullOrEmpty(searchPartnerNumber))
            {
                whereConditions.Add("p.PartnerNumber LIKE @SearchPartnerNumber");
                param.Add("@SearchPartnerNumber", $"%{searchPartnerNumber}%");
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            var query = $@"
            SELECT p.*, u.Email AS CreatedByUserEmail, mu.Email AS ModifiedByUserEmail,
                   COUNT(po.Id) AS PolicyCount, 
                   ISNULL(SUM(po.Amount), 0) AS TotalPolicyAmount
            FROM Partners p
            LEFT JOIN Users u ON p.CreatedByUserId = u.Id
            LEFT JOIN Users mu ON p.ModifiedByUserId = mu.Id
            LEFT JOIN Policies po ON p.Id = po.PartnerId
            {whereClause}
            GROUP BY p.Id, p.FirstName, p.LastName, p.Address, p.PartnerNumber,
                     p.CroatianPIN, p.PartnerTypeId, p.CreatedAtUtc, p.CreatedByUserId, p.ModifiedAtUtc, p.ModifiedByUserId, u.Email, mu.Email,
                     p.IsForeign, p.ExternalCode, p.GenderId, p.IsActive
            ORDER BY p.CreatedAtUtc DESC";

            using var conn = Connection;
            var result = await conn.QueryAsync<Partner>(query, param);
            return result ?? Enumerable.Empty<Partner>();
        }

        /// <summary>
        /// Retrieves a paginated list of partners with optional filtering.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="isActive">Filter by active status. Default is true (active partners only).</param>
        /// <param name="partnerTypeId">Filter by partner type ID.</param>
        /// <param name="searchName">Search by first name or last name (partial match).</param>
        /// <param name="searchOib">Search by Croatian Personal Identification Number (partial match).</param>
        /// <param name="searchPartnerNumber">Search by partner number (partial match).</param>
        /// <returns>A tuple containing the paginated list of partners and the total count of matching records.</returns>
        public async Task<(IEnumerable<Partner> items, int totalCount)> GetAllPartnersPaginatedAsync(int pageNumber = 1, int pageSize = 10, bool? isActive = true, int? partnerTypeId = null, string searchName = null, string searchOib = null, string searchPartnerNumber = null)
        {
            var whereConditions = new List<string>();
            var param = new DynamicParameters();
            param.Add("@PageSize", pageSize);
            param.Add("@PageNumber", pageNumber);

            if (isActive.HasValue)
            {
                whereConditions.Add("p.IsActive = @IsActive");
                param.Add("@IsActive", isActive.Value ? 1 : 0);
            }

            if (partnerTypeId.HasValue)
            {
                whereConditions.Add("p.PartnerTypeId = @PartnerTypeId");
                param.Add("@PartnerTypeId", partnerTypeId.Value);
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                whereConditions.Add("(p.FirstName LIKE @SearchName OR p.LastName LIKE @SearchName)");
                param.Add("@SearchName", $"%{searchName}%");
            }

            if (!string.IsNullOrEmpty(searchOib))
            {
                whereConditions.Add("p.CroatianPIN LIKE @SearchOib");
                param.Add("@SearchOib", $"%{searchOib}%");
            }

            if (!string.IsNullOrEmpty(searchPartnerNumber))
            {
                whereConditions.Add("p.PartnerNumber LIKE @SearchPartnerNumber");
                param.Add("@SearchPartnerNumber", $"%{searchPartnerNumber}%");
            }

            var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            // Get total count
            var countQuery = $@"
                SELECT COUNT(DISTINCT p.Id) FROM Partners p
                {whereClause}";
            using var conn = Connection;
            var totalCount = await conn.ExecuteScalarAsync<int>(countQuery, param);

            // Get paginated results
            var dataQuery = $@"
                SELECT p.*, u.Email AS CreatedByUserEmail, mu.Email AS ModifiedByUserEmail,
                       COUNT(po.Id) AS PolicyCount, 
                       ISNULL(SUM(po.Amount), 0) AS TotalPolicyAmount
                FROM Partners p
                LEFT JOIN Users u ON p.CreatedByUserId = u.Id
                LEFT JOIN Users mu ON p.ModifiedByUserId = mu.Id
                LEFT JOIN Policies po ON p.Id = po.PartnerId
                {whereClause}
                GROUP BY p.Id, p.FirstName, p.LastName, p.Address, p.PartnerNumber,
                         p.CroatianPIN, p.PartnerTypeId, p.CreatedAtUtc, p.CreatedByUserId, p.ModifiedAtUtc, p.ModifiedByUserId, u.Email, mu.Email,
                         p.IsForeign, p.ExternalCode, p.GenderId, p.IsActive
                ORDER BY p.CreatedAtUtc DESC
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var items = await conn.QueryAsync<Partner>(dataQuery, param);
            return (items ?? Enumerable.Empty<Partner>(), totalCount);
        }

        /// <summary>
        /// Retrieves a single partner by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the partner.</param>
        /// <returns>The partner with the specified ID, or null if not found.</returns>
        public async Task<Partner> GetPartnerByIdAsync(int id)
        {
            var query = @"
            SELECT p.*, u.Email AS CreatedByUserEmail, mu.Email AS ModifiedByUserEmail,
                   COUNT(po.Id) AS PolicyCount, 
                   ISNULL(SUM(po.Amount), 0) AS TotalPolicyAmount
            FROM Partners p
            LEFT JOIN Users u ON p.CreatedByUserId = u.Id
            LEFT JOIN Users mu ON p.ModifiedByUserId = mu.Id
            LEFT JOIN Policies po ON p.Id = po.PartnerId
            WHERE p.Id = @Id
            GROUP BY p.Id, p.FirstName, p.LastName, p.Address, p.PartnerNumber, 
                     p.CroatianPIN, p.PartnerTypeId, p.CreatedAtUtc, p.CreatedByUserId, p.ModifiedAtUtc, p.ModifiedByUserId, u.Email, mu.Email,
                     p.IsForeign, p.ExternalCode, p.GenderId, p.IsActive";

            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<Partner>(query, new { Id = id });
        }

        /// <summary>
        /// Performs a soft delete on a partner by marking them as inactive.
        /// </summary>
        /// <param name="id">The unique identifier of the partner to delete.</param>
        /// <param name="modifiedAtUtc">The UTC timestamp when the deletion occurred.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the deletion.</param>
        /// <returns>True if the partner was successfully marked as inactive; otherwise, false.</returns>
        public async Task<bool> SoftDeletePartnerAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Partners SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

        /// <summary>
        /// Restores a soft-deleted partner by marking them as active.
        /// </summary>
        /// <param name="id">The unique identifier of the partner to restore.</param>
        /// <param name="modifiedAtUtc">The UTC timestamp when the restoration occurred.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the restoration.</param>
        /// <returns>True if the partner was successfully restored; otherwise, false.</returns>
        public async Task<bool> RestorePartnerAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Partners SET IsActive = 1, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

        /// <summary>
        /// Creates a new partner in the database.
        /// </summary>
        /// <param name="partner">The partner entity to create.</param>
        /// <returns>True if the partner was successfully created; otherwise, false.</returns>
        public async Task<bool> CreatePartnerAsync(Partner partner)
        {
            var query = @"
            INSERT INTO Partners (FirstName, LastName, Address, PartnerNumber, CroatianPIN, 
                                 PartnerTypeId, CreatedAtUtc, CreatedByUserId, IsForeign, ExternalCode, GenderId)
            VALUES (@FirstName, @LastName, @Address, @PartnerNumber, @CroatianPIN, 
                    @PartnerTypeId, @CreatedAtUtc, @CreatedByUserId, @IsForeign, @ExternalCode, @GenderId)";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, partner);
            return rows > 0;
        }

        /// <summary>
        /// Checks whether an external code is unique (not already assigned to another partner).
        /// </summary>
        /// <param name="code">The external code to check.</param>
        /// <returns>True if the code is unique; otherwise, false.</returns>
        public async Task<bool> IsExternalCodeUniqueAsync(string code)
        {
            var query = "SELECT COUNT(1) FROM Partners WHERE ExternalCode = @Code";
            using var conn = Connection;
            var count = await conn.ExecuteScalarAsync<int>(query, new { Code = code });
            return count == 0;
        }

        /// <summary>
        /// Updates an existing partner's information in the database.
        /// </summary>
        /// <param name="p">The partner entity with updated information.</param>
        public async Task UpdatePartnerAsync(Partner p)
        {
            var query = @"UPDATE Partners SET 
          FirstName = @FirstName, LastName = @LastName, Address = @Address, 
          PartnerNumber = @PartnerNumber, CroatianPIN = @CroatianPIN, 
          PartnerTypeId = @PartnerTypeId, IsForeign = @IsForeign, 
          GenderId = @GenderId, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
          WHERE Id = @Id";
            using var conn = Connection;
            await conn.ExecuteAsync(query, p);
        }

        /// <summary>
        /// Retrieves all available partner types from the database.
        /// </summary>
        /// <returns>A collection of all partner types.</returns>
        public async Task<IEnumerable<PartnerType>> GetPartnerTypesAsync()
        {
            using var conn = Connection;
            var result = await conn.QueryAsync<PartnerType>("SELECT Id, Name FROM PartnerTypes");
            return result ?? Enumerable.Empty<PartnerType>();
        }

        /// <summary>
        /// Retrieves all available gender options from the database.
        /// </summary>
        /// <returns>A collection of all genders.</returns>
        public async Task<IEnumerable<Gender>> GetGendersAsync()
        {
            using var conn = Connection;
            var result = await conn.QueryAsync<Gender>("SELECT * FROM Genders");
            return result ?? Enumerable.Empty<Gender>();
        }
    }
}