namespace TicketSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Identity;

public sealed class TicketSystemDbContext(DbContextOptions<TicketSystemDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SalesParty> SalesParties => Set<SalesParty>();
    public DbSet<TicketSaleDistribution> TicketSaleDistributions => Set<TicketSaleDistribution>();
    public DbSet<CashInventory> CashInventories => Set<CashInventory>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketSystemDbContext).Assembly);
    }
}
