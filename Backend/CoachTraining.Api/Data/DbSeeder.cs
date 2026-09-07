using CoachTraining.Api.Constants;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Data;

/// <summary>
/// One-time startup data seeding: the three business roles, and a default
/// Administrator account so the system is usable before any user exists.
/// Idempotent — safe to run on every startup.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration configuration, ILogger logger)
    {
        foreach (var roleName in Roles.All)
        {
            var exists = await db.Roles.AnyAsync(r => r.Name == roleName);
            if (!exists)
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }
        await db.SaveChangesAsync();

        var hasAdministrator = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AnyAsync(u => u.UserRoles.Any(ur => ur.Role.Name == Roles.Administrator));

        if (hasAdministrator)
        {
            return;
        }

        var administratorRole = await db.Roles.FirstAsync(r => r.Name == Roles.Administrator);
        var initialPassword = configuration["Seed:AdminInitialPassword"] ?? "ChangeMe123!";

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@coachtraining.local",
            FullName = "ผู้ดูแลระบบ",
            IsActive = true,
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, initialPassword);
        adminUser.UserRoles = [new UserRole { Role = administratorRole }];

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        logger.LogWarning(
            "Seeded default Administrator account (username: {Username}). Change its password immediately after first login.",
            adminUser.Username);
    }
}
