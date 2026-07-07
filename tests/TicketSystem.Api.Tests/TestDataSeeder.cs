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
        await db.SaveChangesAsync();
        await SeedStationsAsync(db, services);
        await SeedReferenceDataAsync(db);
        await db.SaveChangesAsync();
        await SalesPartySeeder.SeedAsync(db);

        var clock = services.GetRequiredService<IBusinessClock>();

        await SeedTariffsAsync(db, clock);

        db.AppSettings.Add(new AppSetting
        {
            Key = AppSettingKeys.OnlinePaymentEnabled,
            Value = "false"
        });

        EnsureTicketerStationAssignment(db, clock);
        await db.SaveChangesAsync();
    }

    private static void EnsureTicketerStationAssignment(TicketSystemDbContext db, IBusinessClock clock)
    {
        var addisStationCode = BuildDefaultStationCode(CityNames.AddisAbaba);
        var addisStation = db.Stations.Local.FirstOrDefault(x => x.Code == addisStationCode)
            ?? db.Stations.FirstOrDefault(x => x.Code == addisStationCode);
        if (addisStation is null)
        {
            return;
        }

        if (db.UserStationAssignments.Local.Any(x =>
                x.UserId == TicketerId
                && x.StationId == addisStation.Id
                && x.EndedAtUtc == null))
        {
            return;
        }

        db.UserStationAssignments.Add(new UserStationAssignment
        {
            Id = Guid.NewGuid(),
            UserId = TicketerId,
            StationId = addisStation.Id,
            AssignedAtUtc = clock.UtcNow,
            CreatedAt = clock.UtcNow
        });
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

    private static async Task SeedStationsAsync(TicketSystemDbContext db, IServiceProvider services)
    {
        const string defaultStationName = "Meneharia";
        const string defaultStationNameAm = "መነሓሪያ";
        var clock = services.GetRequiredService<IBusinessClock>();

        var cities = await db.Cities.ToListAsync();
        foreach (var city in cities)
        {
            var code = BuildDefaultStationCode(city.Name);
            if (await db.Stations.AnyAsync(x => x.Code == code))
            {
                continue;
            }

            var stationId = Guid.NewGuid();
            db.Stations.Add(new Station
            {
                Id = stationId,
                CityId = city.Id,
                Name = defaultStationName,
                NameAm = defaultStationNameAm,
                Code = code,
                IsImplicitDefault = true
            });

            if (string.Equals(city.Name, CityNames.AddisAbaba, StringComparison.OrdinalIgnoreCase))
            {
                db.UserStationAssignments.Add(new UserStationAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = TicketerId,
                    StationId = stationId,
                    AssignedAtUtc = clock.UtcNow,
                    CreatedAt = clock.UtcNow
                });
            }
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

    private static async Task SeedReferenceDataAsync(TicketSystemDbContext db)
    {
        if (!await db.Associations.AnyAsync())
        {
            db.Associations.Add(new Association
            {
                Id = Guid.NewGuid(),
                Name = "Default Levy Association",
                Code = "DEFAULT_ASSOC",
                ShortName = "Default",
                IsActive = true
            });
        }

        (string Code, string Name, int Rank)[] levels =
        [
            ("L1", "Level 1", 1),
            ("L2", "Level 2", 2),
            ("L3", "Level 3", 3)
        ];

        foreach (var (code, name, rank) in levels)
        {
            if (await db.BusLevels.AnyAsync(x => x.Code == code))
            {
                continue;
            }

            db.BusLevels.Add(new BusLevel
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Rank = rank
            });
        }

        (string Code, string Name)[] types =
        [
            ("regular", "Regular"),
            ("special", "Special")
        ];

        foreach (var (code, name) in types)
        {
            if (await db.BusTypes.AnyAsync(x => x.Code == code))
            {
                continue;
            }

            db.BusTypes.Add(new BusType
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name
            });
        }
    }

    private static async Task SeedTariffsAsync(TicketSystemDbContext db, IBusinessClock clock)
    {
        var busLevels = await db.BusLevels
            .Where(x => x.IsActive)
            .OrderBy(x => x.Rank)
            .ToListAsync();
        var busTypes = await db.BusTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        foreach (var busLevel in busLevels)
        {
            foreach (var busType in busTypes)
            {
                db.Tariffs.Add(new Tariff
                {
                    Id = Guid.NewGuid(),
                    BusLevelId = busLevel.Id,
                    BusTypeId = busType.Id,
                    RatePerKm = 2.50m,
                    Currency = "ETB",
                    IsActive = true,
                    EffectiveFrom = clock.UtcNow
                });
            }
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
