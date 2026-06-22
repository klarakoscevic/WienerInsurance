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

        public async Task<IEnumerable<Partner>> GetAllPartnersAsync()
        {
            var query = @"
            SELECT p.*, u.Email AS CreatedByUserEmail, mu.Email AS ModifiedByUserEmail,
                   COUNT(po.Id) AS PolicyCount, 
                   ISNULL(SUM(po.Amount), 0) AS TotalPolicyAmount
            FROM Partners p
            LEFT JOIN Users u ON p.CreatedByUserId = u.Id
            LEFT JOIN Users mu ON p.ModifiedByUserId = mu.Id
            LEFT JOIN Policies po ON p.Id = po.PartnerId
            GROUP BY p.Id, p.FirstName, p.LastName, p.Address, p.PartnerNumber, 
                     p.CroatianPIN, p.PartnerTypeId, p.CreatedAtUtc, p.CreatedByUserId, p.ModifiedAtUtc, p.ModifiedByUserId, u.Email, mu.Email,
                     p.IsForeign, p.ExternalCode, p.GenderId
            ORDER BY p.CreatedAtUtc DESC";

            using var conn = Connection;
            return await conn.QueryAsync<Partner>(query);
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
                     p.IsForeign, p.ExternalCode, p.GenderId";

            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<Partner>(query, new { Id = id });
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