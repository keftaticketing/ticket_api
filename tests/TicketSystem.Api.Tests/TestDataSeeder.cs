using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common.Constants;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Identity;
using TicketSystem.Infrastructure.Persistence;

namespace TicketSystem.Api.Tests;

internal static class TestDataSeeder
{
    public static readonly Guid AdminId = DatabaseSeeder.AdminId;
    public static readonly Guid TicketerId = DatabaseSeeder.TicketerId;
    public static readonly Guid AddisAbabaCityId = DatabaseSeeder.AddisAbabaCityId;

    public const string AdminInitialPassword = "Admin123!";
    public const string TicketerInitialPassword = "Ticketer123!";
    public const string AdminWorkingPassword = "Admin123!@#";
    public const string TicketerWorkingPassword = "Ticketer123!@#";

    public static async Task SeedAsync(TicketSystemDbContext db, IServiceProvider services)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        await SeedRolesAsync(services);
        await SeedUsersAsync(services);
        await SeedCitiesAsync(db);
        await SeedStationsAsync(db);
        await SalesPartySeeder.SeedAsync(db);

        var clock = services.GetRequiredService<IBusinessClock>();

        db.Tariffs.Add(new Tariff
        {
            Id = Guid.NewGuid(),
            RatePerKm = 2.50m,
            Currency = "ETB",
            IsActive = true,
            EffectiveFrom = clock.UtcNow
        });

        db.AppSettings.Add(new AppSetting
        {
            Key = AppSettingKeys.OnlinePaymentEnabled,
            Value = "false"
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedCitiesAsync(TicketSystemDbContext db)
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
            if (await db.Cities.AnyAsync(x => x.Name == name))
            {
                continue;
            }

            db.Cities.Add(new City
            {
                Id = name == CityNames.AddisAbaba ? AddisAbabaCityId : Guid.NewGuid(),
                Name = name,
                DistanceFromAddisKm = distanceFromAddisKm
            });
        }
    }

    private static async Task SeedStationsAsync(TicketSystemDbContext db)
    {
        const string defaultStationName = "Meneharia";
        const string defaultStationNameAm = "መነሓሪያ";

        var cities = await db.Cities.ToListAsync();
        foreach (var city in cities)
        {
            var code = BuildDefaultStationCode(city.Name);
            if (await db.Stations.AnyAsync(x => x.Code == code))
            {
                continue;
            }

            db.Stations.Add(new Station
            {
                Id = Guid.NewGuid(),
                CityId = city.Id,
                Name = defaultStationName,
                NameAm = defaultStationNameAm,
                Code = code,
                IsImplicitDefault = true
            });
        }
    }

    private static string BuildDefaultStationCode(string cityName)
    {
        Span<char> buffer = stackalloc char[cityName.Length];
        var len = 0;
        foreach (var ch in cityName.ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[len++] = ch;
                continue;
            }

            if (len > 0 && buffer[len - 1] != '_')
            {
                buffer[len++] = '_';
            }
        }

        var normalized = new string(buffer[..len]).TrimEnd('_');
        return $"{normalized}_MAIN";
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

    private static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = new ApplicationUser
        {
            Id = AdminId,
            UserName = "admin",
            FullName = "System Admin",
            IsActive = true,
            MustChangePassword = false,
            Email = "admin@ticketsystem.local",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, AdminWorkingPassword);
        await userManager.AddToRoleAsync(admin, RoleNames.Admin);

        var ticketer = new ApplicationUser
        {
            Id = TicketerId,
            UserName = "ticketer",
            FullName = "Counter Ticketer",
            IsActive = true,
            MustChangePassword = false,
            Email = "ticketer@ticketsystem.local",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(ticketer, TicketerWorkingPassword);
        await userManager.AddToRoleAsync(ticketer, RoleNames.Ticketer);
    }
}
