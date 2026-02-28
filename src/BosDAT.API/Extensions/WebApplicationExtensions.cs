using Microsoft.AspNetCore.Identity;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BosDAT.API.Extensions;

/// <summary>
/// Extension methods for configuring the WebApplication at startup.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Initializes the database by running pending migrations, creating default roles, and seeding default users.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            await EnsureRolesAsync(roleManager);

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            await EnsureAdminUserAsync(userManager, app.Configuration, logger);
            await EnsureWorkerUserAsync(userManager, app.Configuration, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }

    /// <summary>
    /// Creates default application roles if they don't already exist.
    /// </summary>
    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { "Admin", "FinancialAdmin", "Teacher", "Student", "Staff", "User", "Worker" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    /// <summary>
    /// Creates the default admin user (admin@bosdat.nl) if it doesn't exist.
    /// </summary>
    private static async Task EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var adminEmail = "admin@bosdat.nl";
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var adminPassword = configuration["AdminSettings:DefaultPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("AdminSettings:DefaultPassword not configured. Skipping default admin user creation.");
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            DisplayName = "System Administrator",
            AccountStatus = BosDAT.Core.Enums.AccountStatus.Active,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    /// <summary>
    /// Creates the background worker service account if it doesn't exist.
    /// </summary>
    private static async Task EnsureWorkerUserAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var workerEmail = configuration["WorkerSettings:Email"] ?? "worker@bosdat.nl";
        if (await userManager.FindByEmailAsync(workerEmail) is not null)
            return;

        var workerPassword = configuration["WorkerSettings:Password"];
        if (string.IsNullOrEmpty(workerPassword))
            return;

        var workerUser = new ApplicationUser
        {
            UserName = workerEmail,
            Email = workerEmail,
            FirstName = "Background",
            LastName = "Worker",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(workerUser, workerPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(workerUser, ["Worker", "Admin"]);
            logger.LogInformation("Background worker service account created: {Email}", workerEmail);
        }
    }
}
