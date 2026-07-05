namespace TicketSystem.Application.Abstractions.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TicketSystem.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<Association> Associations { get; }
    DbSet<BusLevel> BusLevels { get; }
    DbSet<BusType> BusTypes { get; }
    DbSet<Bus> Buses { get; }
    DbSet<City> Cities { get; }
    DbSet<Station> Stations { get; }
    DbSet<UserStationAssignment> UserStationAssignments { get; }
    DbSet<Route> Routes { get; }
    DbSet<Tariff> Tariffs { get; }
    DbSet<Schedule> Schedules { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SalesParty> SalesParties { get; }
    DbSet<TicketSaleDistribution> TicketSaleDistributions { get; }
    DbSet<CashInventory> CashInventories { get; }
    DbSet<CashLedgerEntry> CashLedgerEntries { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
