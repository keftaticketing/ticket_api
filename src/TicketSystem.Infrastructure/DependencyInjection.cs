namespace TicketSystem.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Infrastructure.Identity;
using TicketSystem.Infrastructure.Persistence;
using TicketSystem.Infrastructure.Persistence.Interceptors;
using TicketSystem.Infrastructure.Realtime;
using TicketSystem.Infrastructure.Time;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IBusinessClock, NodaBusinessClock>();
        services.AddSingleton<ISeatEventPublisher, SeatEventHub>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditingSaveChangesInterceptor>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TicketSystemDbContext>()
            .AddSignInManager<SignInManager<ApplicationUser>>();

        services.AddDbContext<TicketSystemDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingSaveChangesInterceptor>());

            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsEnvironment("Testing"))
            {
                return;
            }

            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<TicketSystemDbContext>());

        services.AddScoped<IIdentityAuthService, IdentityAuthService>();
        services.AddScoped<IIdentityUserService, IdentityUserService>();
        services.AddScoped<IIdentityAccountService, IdentityAccountService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
