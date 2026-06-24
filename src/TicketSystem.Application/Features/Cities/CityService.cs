namespace TicketSystem.Application.Features.Cities;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Cities;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;

public interface ICityService
{
    Task<ErrorOr<CityResponse>> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<CityResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<CityResponse>>> GetDestinationsAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<CityResponse>>> SearchAsync(string? query, CancellationToken cancellationToken = default);
}

public sealed class CityService(IApplicationDbContext db, IBusinessClock clock) : ICityService
{
    public async Task<ErrorOr<CityResponse>> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CityNameRequired;
        }

        if (!IsValidDistance(name, request.DistanceFromAddisKm))
        {
            return DomainErrors.InvalidCityDistance;
        }

        if (IsAddisAbaba(name) && await AddisAbabaExistsAsync(cancellationToken))
        {
            return DomainErrors.DuplicateCity;
        }

        if (await NameExistsAsync(name, cancellationToken))
        {
            return DomainErrors.DuplicateCity;
        }

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = name,
            DistanceFromAddisKm = request.DistanceFromAddisKm
        };

        db.Cities.Add(city);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return DomainErrors.DuplicateCity;
        }

        return Map(city);
    }

    public async Task<ErrorOr<IReadOnlyList<CityResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cities = await db.Cities.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return cities.Select(Map).ToList();
    }

    public async Task<ErrorOr<IReadOnlyList<CityResponse>>> GetDestinationsAsync(CancellationToken cancellationToken = default)
    {
        var cities = await db.Cities.AsNoTracking()
            .Where(x => x.IsActive && x.Name != CityNames.AddisAbaba)
            .OrderBy(x => x.DistanceFromAddisKm)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return cities.Select(Map).ToList();
    }

    public async Task<ErrorOr<IReadOnlyList<CityResponse>>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var cities = await db.Cities.AsNoTracking()
            .Where(x => x.IsActive && x.Name != CityNames.AddisAbaba)
            .OrderBy(x => x.DistanceFromAddisKm)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            cities = cities
                .Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return cities.Select(Map).ToList();
    }

    private async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken)
    {
        var cities = await db.Cities.AsNoTracking().Select(x => x.Name).ToListAsync(cancellationToken);
        return cities.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> AddisAbabaExistsAsync(CancellationToken cancellationToken) =>
        await db.Cities.AsNoTracking().AnyAsync(x => x.Name == CityNames.AddisAbaba, cancellationToken);

    private static bool IsAddisAbaba(string name) =>
        string.Equals(name, CityNames.AddisAbaba, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidDistance(string name, decimal distanceFromAddisKm) =>
        IsAddisAbaba(name) ? distanceFromAddisKm == 0 : distanceFromAddisKm > 0;

    private CityResponse Map(City city) =>
        new(city.Id, city.Name, city.DistanceFromAddisKm, city.IsActive, clock.ToLocalDateTime(city.CreatedAt));
}
