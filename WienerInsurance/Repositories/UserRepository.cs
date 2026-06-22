using Dapper;
using Microsoft.Data.SqlClient;
using WienerInsurance.Models;

public class UserRepository
{
    private readonly string _conn;
    public UserRepository(string connectionString) => _conn = connectionString;

    public async Task<User> GetUserByEmailAsync(string email)
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryFirstOrDefaultAsync<User>(
            "SELECT u.* FROM Users u JOIN UserRoles r ON u.RoleId = r.Id WHERE Email = @Email",
            new { Email = email });
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryAsync<User>("SELECT u.* FROM Users u JOIN UserRoles r ON u.RoleId = r.Id");
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"INSERT INTO Users (Email, FirstName, LastName, PasswordHash, RoleId)
                    OUTPUT INSERTED.Id
                    VALUES (@Email, @FirstName, @LastName, @PasswordHash, @RoleId)";
        return await db.ExecuteScalarAsync<int>(sql, user);
    }


    public async Task<IEnumerable<UserRole>> GetAllRolesAsync()
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryAsync<UserRole>("SELECT * FROM UserRoles");
    }

    public async Task<IEnumerable<string>> GetAllAdminEmailsAsync()
    {
        using var db = new SqlConnection(_conn);

        string sql = @"
        SELECT u.Email 
        FROM Users u
        JOIN UserRoles r ON u.RoleId = r.Id
        WHERE r.Name = 'Admin'"; 

        return await db.QueryAsync<string>(sql);
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateUserAsync(User user)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET Email = @Email, FirstName = @FirstName, LastName = @LastName, PasswordHash = @PasswordHash, RoleId = @RoleId WHERE Id = @Id";
        await db.ExecuteAsync(sql, user);
    }
}
