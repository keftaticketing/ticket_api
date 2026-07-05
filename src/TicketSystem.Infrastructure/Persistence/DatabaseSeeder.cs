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
    private const string DefaultStationName = "Meneharia";
    private const string DefaultStationNameAm = "መነሓሪያ";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketSystemDbContext>();
        await db.Database.MigrateAsync();

        await SeedRolesAsync(scope.ServiceProvider);
        await SeedUsersAsync(scope.ServiceProvider, logger);
        await SeedCitiesAsync(db, logger);
        await SeedStationsAsync(db, logger);
        await BackfillRouteStationsAsync(db, logger);
        await SeedAssociationsAsync(db, logger);
        await SeedBusLevelsAsync(db, logger);
        await SeedBusTypesAsync(db, logger);
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
        await SeedUserStationAssignmentsAsync(db, logger, clock);
        await SalesPartySeeder.SeedAsync(db);

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

    private static async Task SeedStationsAsync(TicketSystemDbContext db, ILogger logger)
    {
        var cities = await db.Cities.ToListAsync();
        foreach (var city in cities)
        {
            var code = BuildDefaultStationCode(city.Name);
            var existing = await db.Stations.SingleOrDefaultAsync(x => x.Code == code);
            if (existing is null)
            {
                db.Stations.Add(new Station
                {
                    Id = Guid.NewGuid(),
                    CityId = city.Id,
                    Name = DefaultStationName,
                    NameAm = DefaultStationNameAm,
                    Code = code,
                    IsImplicitDefault = true
                });
                logger.LogInformation("Seeded default station {StationCode} for city {CityName}.", code, city.Name);
                continue;
            }

            existing.CityId = city.Id;
            existing.Name = DefaultStationName;
            existing.NameAm = DefaultStationNameAm;
            existing.IsActive = true;
            existing.IsImplicitDefault = true;
        }
    }

    private static async Task SeedAssociationsAsync(TicketSystemDbContext db, ILogger logger)
    {
        if (await db.Associations.AnyAsync())
        {
            return;
        }

        db.Associations.Add(new Association
        {
            Id = Guid.NewGuid(),
            Name = "Default Levy Association",
            Code = "DEFAULT_ASSOC",
            ShortName = "Default",
            IsActive = true
        });

        logger.LogInformation("Seeded default association.");
    }

    private static async Task SeedBusLevelsAsync(TicketSystemDbContext db, ILogger logger)
    {
        (string Code, string Name, int Rank)[] levels =
        [
            ("L1", "Level 1", 1),
            ("L2", "Level 2", 2),
            ("L3", "Level 3", 3)
        ];

        foreach (var (code, name, rank) in levels)
        {
            var existing = await db.BusLevels.SingleOrDefaultAsync(x => x.Code == code);
            if (existing is null)
            {
                db.BusLevels.Add(new BusLevel
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Rank = rank
                });
                logger.LogInformation("Seeded bus level {BusLevelCode}.", code);
                continue;
            }

            existing.Name = name;
            existing.Rank = rank;
            existing.IsActive = true;
        }
    }

    private static async Task SeedBusTypesAsync(TicketSystemDbContext db, ILogger logger)
    {
        (string Code, string Name)[] types =
        [
            ("regular", "Regular"),
            ("special", "Special")
        ];

        foreach (var (code, name) in types)
        {
            var existing = await db.BusTypes.SingleOrDefaultAsync(x => x.Code == code);
            if (existing is null)
            {
                db.BusTypes.Add(new BusType
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name
                });
                logger.LogInformation("Seeded bus type {BusTypeCode}.", code);
                continue;
            }

            existing.Name = name;
            existing.IsActive = true;
        }
    }

    private static async Task BackfillRouteStationsAsync(TicketSystemDbContext db, ILogger logger)
    {
        var routesNeedingStations = await db.Routes
            .Where(x => x.FromStationId == Guid.Empty || x.ToStationId == Guid.Empty)
            .ToListAsync();
        if (routesNeedingStations.Count == 0)
        {
            return;
        }

        var defaultStations = await db.Stations
            .Where(x => x.IsImplicitDefault && x.IsActive)
            .ToDictionaryAsync(x => x.CityId);

        foreach (var route in routesNeedingStations)
        {
            if (!defaultStations.TryGetValue(route.FromCityId, out var fromStation)
                || !defaultStations.TryGetValue(route.ToCityId, out var toStation))
            {
                logger.LogWarning(
                    "Skipping route {RouteId} backfill because default stations are missing.",
                    route.Id);
                continue;
            }

            route.FromStationId = fromStation.Id;
            route.ToStationId = toStation.Id;
            logger.LogInformation(
                "Backfilled route {RouteId} with stations {FromStationCode} -> {ToStationCode}.",
                route.Id,
                fromStation.Code,
                toStation.Code);
        }
    }

    private static async Task SeedUserStationAssignmentsAsync(
        TicketSystemDbContext db,
        ILogger logger,
        IBusinessClock clock)
    {
        var addisStationCode = BuildDefaultStationCode(CityNames.AddisAbaba);
        var addisStation = await db.Stations
            .SingleOrDefaultAsync(x => x.Code == addisStationCode);
        if (addisStation is null)
        {
            return;
        }

        var existing = await db.UserStationAssignments
            .SingleOrDefaultAsync(x =>
                x.UserId == TicketerId
                && x.StationId == addisStation.Id
                && x.EndedAtUtc == null);
        if (existing is not null)
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

        logger.LogInformation(
            "Seeded active station assignment for ticketer {TicketerId} at station {StationCode}.",
            TicketerId,
            addisStationCode);
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
