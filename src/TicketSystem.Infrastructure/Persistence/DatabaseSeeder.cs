namespace TicketSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common.Constants;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Identity;

public static class DatabaseSeeder
{
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TicketerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AddisAbabaCityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketSystemDbContext>();
        await db.Database.MigrateAsync();

        await SeedRolesAsync(scope.ServiceProvider);
        await SeedUsersAsync(scope.ServiceProvider, logger);
        await SeedCitiesAsync(db, logger);
        await SalesPartySeeder.SeedAsync(db);

        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();

        if (!await db.Tariffs.AnyAsync())
        {
            db.Tariffs.Add(new Tariff
            {
                Id = Guid.NewGuid(),
                RatePerKm = 2.50m,
                Currency = "ETB",
                IsActive = true,
                EffectiveFrom = clock.UtcNow
            });
            logger.LogInformation("Seeded default tariff (2.50 ETB/km).");
        }

        if (!await db.AppSettings.AnyAsync(x => x.Key == AppSettingKeys.OnlinePaymentEnabled))
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKeys.OnlinePaymentEnabled,
                Value = "false"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCitiesAsync(TicketSystemDbContext db, ILogger logger)
    {
        (string Name, decimal DistanceFromAddisKm)[] cities =
        [
            (CityNames.AddisAbaba, 0),
            ("Adama", 99),
            ("Hawassa", 275),
            ("Jimma", 346),
            ("Dire Dawa", 515),
            ("Bahir Dar", 565),
            ("Gondar", 748),
            ("Mekelle", 783)
        ];

        foreach (var (name, distanceFromAddisKm) in cities)
        {
            var existing = await db.Cities.SingleOrDefaultAsync(x => x.Name == name);
            if (existing is null)
            {
                db.Cities.Add(new City
                {
                    Id = name == CityNames.AddisAbaba ? AddisAbabaCityId : Guid.NewGuid(),
                    Name = name,
                    DistanceFromAddisKm = distanceFromAddisKm
                });
                logger.LogInformation("Seeded city {CityName} ({DistanceKm} km from Addis Ababa).", name, distanceFromAddisKm);
                continue;
            }

            existing.DistanceFromAddisKm = distanceFromAddisKm;
            existing.IsActive = true;
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(RoleNames.Admin) { Id = Guid.NewGuid() });
        }

        if (!await roleManager.RoleExistsAsync(RoleNames.Ticketer))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(RoleNames.Ticketer) { Id = Guid.NewGuid() });
        }
    }

    private static async Task SeedUsersAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByNameAsync("admin") is null)
        {
            var admin = new ApplicationUser
            {
                Id = AdminId,
                UserName = "admin",
                FullName = "System Admin",
                IsActive = true,
                MustChangePassword = true,
                Email = "admin@ticketsystem.local",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            logger.LogInformation("Seeded admin user.");
        }

        if (await userManager.FindByNameAsync("ticketer") is null)
        {
            var ticketer = new ApplicationUser
            {
                Id = TicketerId,
                UserName = "ticketer",
                FullName = "Counter Ticketer",
                IsActive = true,
                MustChangePassword = true,
                Email = "ticketer@ticketsystem.local",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(ticketer, "Ticketer123!");
            await userManager.AddToRoleAsync(ticketer, RoleNames.Ticketer);
            logger.LogInformation("Seeded ticketer user.");
        }
    }
}
