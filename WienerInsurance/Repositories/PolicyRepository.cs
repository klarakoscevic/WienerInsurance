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
                    INSERT INTO Policies (PartnerId, PolicyNumber, Amount)
                    VALUES (@PartnerId, @PolicyNumber, @Amount)";

            using var conn = Connection;
            var rows = await conn.ExecuteAsync(query, policy);
            return rows > 0;
        }

        public async Task<IEnumerable<Policy>> GetAllPoliciesAsync()
        {
            var query = @"
                    SELECT p.*, (part.FirstName + ' ' + part.LastName) AS PartnerFullName 
                    FROM Policies p 
                    LEFT JOIN Partners part ON p.PartnerId = part.Id";

            using var conn = Connection;
            return await conn.QueryAsync<Policy>(query);
        }      
    }
}
