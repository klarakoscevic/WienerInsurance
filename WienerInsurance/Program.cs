using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using WienerInsurance.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton(new PartnerRepository(connectionString));
builder.Services.AddSingleton(new PolicyRepository(connectionString));
builder.Services.AddSingleton(new UserRepository(connectionString));

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

            var adminEmail = "kkoscevic@gmail.com";
            var admin = await conn.QueryFirstOrDefaultAsync<int?>("SELECT Id FROM Users WHERE Email = @Email", new { Email = adminEmail });
            if (admin == null)
            {
                var adminRoleId = await conn.QueryFirstOrDefaultAsync<int>("SELECT Id FROM UserRoles WHERE Name = @Name", new { Name = "Admin" });
                var hasher = new PasswordHasher<string>();
                var hash = hasher.HashPassword(null, "Admin123!");
                await conn.ExecuteAsync(@"INSERT INTO Users (Email, FirstName, LastName, PasswordHash, RoleId)
                                         VALUES (@Email, @FirstName, @LastName, @PasswordHash, @RoleId)",
                    new { Email = adminEmail, FirstName = "Klara", LastName = "Koscevic", PasswordHash = hash, RoleId = adminRoleId });
            }
        }
        catch
        {
            // ignore seeding errors to avoid startup crash; log if you add logging
        }
    });
}

SeedAsync().GetAwaiter().GetResult();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();