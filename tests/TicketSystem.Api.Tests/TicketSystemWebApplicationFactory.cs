using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketSystem.Infrastructure.Persistence;

namespace TicketSystem.Api.Tests;

public sealed class TicketSystemWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    public HttpClient CreateClientWithCredentials(string username, string password)
    {
        var client = CreateClient();
        var loginResponse = client.PostAsJsonAsync("/api/auth/login", new { username, password }).GetAwaiter().GetResult();
        loginResponse.EnsureSuccessStatusCode();
        var login = loginResponse.Content.ReadFromJsonAsync<LoginPayload>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Login payload missing.");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketSystemDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedAsync(db, scope.ServiceProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TicketSystemDbContext>>();
            services.RemoveAll<TicketSystemDbContext>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<TicketSystemDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        _ = CreateClient();
        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private sealed record LoginPayload(string AccessToken, int ExpiresIn, string Role, string FullName);
}
