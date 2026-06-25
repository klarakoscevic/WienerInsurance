using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WienerInsurance.Models;

namespace WienerInsurance.Repositories
{
    public class PartnerRepository
    {
        private readonly string _connectionString;

        public PartnerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

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
            return await conn.QueryAsync<Partner>(query, param);
        }

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
            return (items, totalCount);
        }

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

        public async Task<bool> SoftDeletePartnerAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Partners SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

        public async Task<bool> RestorePartnerAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
        {
            var query = @"
                UPDATE Partners SET IsActive = 1, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
            return rows > 0;
        }

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

        public async Task<bool> IsExternalCodeUniqueAsync(string code)
        {
            var query = "SELECT COUNT(1) FROM Partners WHERE ExternalCode = @Code";
            using var conn = Connection;
            var count = await conn.ExecuteScalarAsync<int>(query, new { Code = code });
            return count == 0;
        }

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

        public async Task<IEnumerable<PartnerType>> GetPartnerTypesAsync()
        {
            using var conn = Connection;
            return await conn.QueryAsync<PartnerType>("SELECT Id, Name FROM PartnerTypes");
        }

        public async Task<IEnumerable<Gender>> GetGendersAsync()
        {
            using var conn = Connection;
            return await conn.QueryAsync<Gender>("SELECT * FROM Genders");
        }
    }
}