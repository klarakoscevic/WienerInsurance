using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
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

    public async Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null, int? roleId = null, string searchName = null)
    {
        using var db = new SqlConnection(_conn);
        var whereConditions = new List<string>();
        var param = new DynamicParameters();

        if (isActive.HasValue)
        {
            whereConditions.Add("ISNULL(u.IsActive, 1) = @IsActive");
            param.Add("@IsActive", isActive.Value ? 1 : 0);
        }

        if (roleId.HasValue)
        {
            whereConditions.Add("u.RoleId = @RoleId");
            param.Add("@RoleId", roleId.Value);
        }

        if (!string.IsNullOrEmpty(searchName))
        {
            whereConditions.Add("(u.FirstName LIKE @SearchName OR u.LastName LIKE @SearchName)");
            param.Add("@SearchName", $"%{searchName}%");
        }

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        var sql = $"SELECT u.* FROM Users u JOIN UserRoles r ON u.RoleId = r.Id {whereClause}";
        return await db.QueryAsync<User>(sql, param);
    }

    public async Task<(IEnumerable<User> items, int totalCount)> GetAllUsersPaginatedAsync(int pageNumber = 1, int pageSize = 10, bool? isActive = true, int? roleId = null, string searchName = null)
    {
        using var db = new SqlConnection(_conn);
        var whereConditions = new List<string>();
        var param = new DynamicParameters();
        param.Add("@PageSize", pageSize);
        param.Add("@PageNumber", pageNumber);

        if (isActive.HasValue)
        {
            whereConditions.Add("ISNULL(u.IsActive, 1) = @IsActive");
            param.Add("@IsActive", isActive.Value ? 1 : 0);
        }

        if (roleId.HasValue)
        {
            whereConditions.Add("u.RoleId = @RoleId");
            param.Add("@RoleId", roleId.Value);
        }

        if (!string.IsNullOrEmpty(searchName))
        {
            whereConditions.Add("(u.FirstName LIKE @SearchName OR u.LastName LIKE @SearchName)");
            param.Add("@SearchName", $"%{searchName}%");
        }

        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Get total count
        var countQuery = $"SELECT COUNT(*) FROM Users u JOIN UserRoles r ON u.RoleId = r.Id {whereClause}";
        var totalCount = await db.ExecuteScalarAsync<int>(countQuery, param);

        // Get paginated results
        var dataQuery = $@"
            SELECT u.* FROM Users u 
            JOIN UserRoles r ON u.RoleId = r.Id 
            {whereClause}
            ORDER BY u.Id DESC
            OFFSET (@PageNumber - 1) * @PageSize ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var items = await db.QueryAsync<User>(dataQuery, param);
        return (items, totalCount);
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"INSERT INTO Users (Email, FirstName, LastName, PasswordHash, RoleId, CreatedAtUtc, CreatedByUserId, ModifiedAtUtc, ModifiedByUserId, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@Email, @FirstName, @LastName, @PasswordHash, @RoleId, @CreatedAtUtc, @CreatedByUserId, @ModifiedAtUtc, @ModifiedByUserId, @IsActive)";
        return await db.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task<bool> SoftDeleteUserAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id";
        var rows = await db.ExecuteAsync(sql, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
        return rows > 0;
    }

    public async Task<bool> RestoreUserAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET IsActive = 1, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id";
        var rows = await db.ExecuteAsync(sql, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
        return rows > 0;
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
        var sql = @"UPDATE Users SET Email = @Email, FirstName = @FirstName, LastName = @LastName, PasswordHash = @PasswordHash, RoleId = @RoleId,
                    ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id";
        await db.ExecuteAsync(sql, user);
    }
}
