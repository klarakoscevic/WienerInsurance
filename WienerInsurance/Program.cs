using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using WienerInsurance.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped(_ => new PartnerRepository(connectionString));
builder.Services.AddScoped(_ => new PolicyRepository(connectionString));
builder.Services.AddScoped(_ => new UserRepository(connectionString));

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/AccessDenied";
    });

var app = builder.Build();

// Seed roles and initial admin user if missing
Task SeedAsync()
{
    return Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("DefaultConnection");
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            var existingRoles = (await conn.QueryAsync<string>("SELECT Name FROM UserRoles")).ToList();
            if (!existingRoles.Contains("Admin"))
                await conn.ExecuteAsync("INSERT INTO UserRoles (Name) VALUES (@Name)", new { Name = "Admin" });
            if (!existingRoles.Contains("User"))
                await conn.ExecuteAsync("INSERT INTO UserRoles (Name) VALUES (@Name)", new { Name = "User" });

            // Get admin email from configuration
            var configAdminEmail = config["DefaultAdminEmail"] ?? "kkoscevic@gmail.com";
            var admin = await conn.QueryFirstOrDefaultAsync<int?>("SELECT Id FROM Users WHERE Email = @Email", new { Email = configAdminEmail });
            if (admin == null)
            {
                var adminRoleId = await conn.QueryFirstOrDefaultAsync<int>("SELECT Id FROM UserRoles WHERE Name = @Name", new { Name = "Admin" });
                    var hasher = new PasswordHasher<string>();

                    // Get password from configuration
                    var adminPassword = config["DefaultAdminPassword"] ?? "Admin123!";
                    var hash = hasher.HashPassword(null, adminPassword);

                    // Get admin email from configuration
                    var adminEmail = config["DefaultAdminEmail"] ?? "kkoscevic@gmail.com";

                    await conn.ExecuteAsync(@"INSERT INTO Users (Email, FirstName, LastName, PasswordHash, RoleId)
                                             VALUES (@Email, @FirstName, @LastName, @PasswordHash, @RoleId)",
                        new { Email = adminEmail, FirstName = "System", LastName = "Administrator", PasswordHash = hash, RoleId = adminRoleId });
            }
        }
        catch (Exception ex)
        {
            // Log seeding errors but don't crash the application
            Console.WriteLine($"Error during database seeding: {ex.Message}");
        }
    });
}

SeedAsync().GetAwaiter().GetResult();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();