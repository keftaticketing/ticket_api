using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.Api.Cors;
using TicketSystem.Api.Json;
using TicketSystem.Api.Middleware;
using TicketSystem.Api.Options;
using TicketSystem.Application;
using TicketSystem.Infrastructure;
using TicketSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDecimalJsonConverter());
    });
builder.Services.AddOpenApi();

var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.Configure<SseOptions>(builder.Configuration.GetSection(SseOptions.SectionName));
builder.Services.Configure<MobileClientOptions>(builder.Configuration.GetSection(MobileClientOptions.SectionName));
builder.Services.AddTicketSystemCors(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Ticket System API");
    });
}

app.UseCors(CorsPolicySetup.DefaultPolicyName);
app.UseMiddleware<MobileClientMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseSeeder.SeedAsync(app.Services, app.Logger);
}

app.Run();

public partial class Program;
