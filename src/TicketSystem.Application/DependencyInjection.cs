namespace TicketSystem.Application;

using Microsoft.Extensions.DependencyInjection;
using TicketSystem.Application.Features.Cities;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Application.Features.Buses;
using TicketSystem.Application.Features.Routes;
using TicketSystem.Application.Features.SalesParties;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Application.Features.Settings;
using TicketSystem.Application.Features.Tariffs;
using TicketSystem.Application.Features.Reports;
using TicketSystem.Application.Features.Tickets;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBusService, BusService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<ITariffService, TariffService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISalesPartyService, SalesPartyService>();
        services.AddScoped<ICashInventoryService, CashInventoryService>();
        services.AddScoped<ITicketSaleDistributionWriter, TicketSaleDistributionWriter>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IReportsService, ReportsService>();

        return services;
    }
}
