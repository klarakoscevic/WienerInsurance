using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WienerInsurance.Models;

/// <summary>
/// Repository for managing user data persistence operations.
/// </summary>
public class UserRepository
{
    private readonly string _conn;

    /// <summary>
    /// Initializes a new instance of the UserRepository class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public UserRepository(string connectionString) => _conn = connectionString;

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user to retrieve.</param>
    /// <returns>The user with the specified email, or null if not found.</returns>
    public async Task<User> GetUserByEmailAsync(string email)
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryFirstOrDefaultAsync<User>(
            "SELECT u.* FROM Users u JOIN UserRoles r ON u.RoleId = r.Id WHERE Email = @Email",
            new { Email = email });
    }

    /// <summary>
    /// Retrieves all users from the database with optional filtering.
    /// </summary>
    /// <param name="isActive">Filter by active status. Null returns all users regardless of status.</param>
    /// <param name="roleId">Filter by role ID.</param>
    /// <param name="searchName">Search by first name or last name (partial match).</param>
    /// <returns>A collection of users matching the filter criteria.</returns>
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
        var result = await db.QueryAsync<User>(sql, param);
        return result ?? Enumerable.Empty<User>();
    }

    /// <summary>
    /// Retrieves a paginated list of users with optional filtering.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="isActive">Filter by active status. Default is true (active users only).</param>
    /// <param name="roleId">Filter by role ID.</param>
    /// <param name="searchName">Search by first name or last name (partial match).</param>
    /// <returns>A tuple containing the paginated list of users and the total count of matching records.</returns>
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
        return (items ?? Enumerable.Empty<User>(), totalCount);
    }

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The ID of the newly created user.</returns>
    public async Task<int> CreateUserAsync(User user)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"INSERT INTO Users (Email, FirstName, LastName, PasswordHash, RoleId, CreatedAtUtc, CreatedByUserId, ModifiedAtUtc, ModifiedByUserId, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@Email, @FirstName, @LastName, @PasswordHash, @RoleId, @CreatedAtUtc, @CreatedByUserId, @ModifiedAtUtc, @ModifiedByUserId, @IsActive)";
        return await db.ExecuteScalarAsync<int>(sql, user);
    }

    /// <summary>
    /// Performs a soft delete on a user by marking them as inactive.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="modifiedAtUtc">The UTC timestamp when the deletion occurred.</param>
    /// <param name="modifiedByUserId">The ID of the user performing the deletion.</param>
    /// <returns>True if the user was successfully marked as inactive; otherwise, false.</returns>
    public async Task<bool> SoftDeleteUserAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET IsActive = 0, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id";
        var rows = await db.ExecuteAsync(sql, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
        return rows > 0;
    }

    /// <summary>
    /// Restores a soft-deleted user by marking them as active.
    /// </summary>
    /// <param name="id">The unique identifier of the user to restore.</param>
    /// <param name="modifiedAtUtc">The UTC timestamp when the restoration occurred.</param>
    /// <param name="modifiedByUserId">The ID of the user performing the restoration.</param>
    /// <returns>True if the user was successfully restored; otherwise, false.</returns>
    public async Task<bool> RestoreUserAsync(int id, DateTime modifiedAtUtc, int? modifiedByUserId)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET IsActive = 1, ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id";
        var rows = await db.ExecuteAsync(sql, new { Id = id, ModifiedAtUtc = modifiedAtUtc, ModifiedByUserId = modifiedByUserId });
        return rows > 0;
    }


    /// <summary>
    /// Retrieves all available user roles from the database.
    /// </summary>
    /// <returns>A collection of all user roles.</returns>
    public async Task<IEnumerable<UserRole>> GetAllRolesAsync()
    {
        using var db = new SqlConnection(_conn);
        var result = await db.QueryAsync<UserRole>("SELECT * FROM UserRoles");
        return result ?? Enumerable.Empty<UserRole>();
    }

    /// <summary>
    /// Retrieves the email addresses of all users with the Admin role.
    /// </summary>
    /// <returns>A collection of email addresses for all admin users.</returns>
    public async Task<IEnumerable<string>> GetAllAdminEmailsAsync()
    {
        using var db = new SqlConnection(_conn);

        string sql = @"
        SELECT u.Email 
        FROM Users u
        JOIN UserRoles r ON u.RoleId = r.Id
        WHERE r.Name = 'Admin'"; 

        var result = await db.QueryAsync<string>(sql);
        return result ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>The user with the specified ID, or null if not found.</returns>
    public async Task<User> GetUserByIdAsync(int id)
    {
        using var db = new SqlConnection(_conn);
        return await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// Updates an existing user's information in the database.
    /// </summary>
    /// <param name="user">The user entity with updated information.</param>
    public async Task UpdateUserAsync(User user)
    {
        using var db = new SqlConnection(_conn);
        var sql = @"UPDATE Users SET Email = @Email, FirstName = @FirstName, LastName = @LastName, PasswordHash = @PasswordHash, RoleId = @RoleId,
                    ModifiedAtUtc = @ModifiedAtUtc, ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id";
        await db.ExecuteAsync(sql, user);
    }
}
