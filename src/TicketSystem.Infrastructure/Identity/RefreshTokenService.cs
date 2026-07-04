namespace TicketSystem.Infrastructure.Identity;

using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Errors;
using TicketSystem.Infrastructure.Persistence;

public sealed class RefreshTokenService(
    TicketSystemDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : IRefreshTokenService
{
    public async Task<(string RefreshToken, int ExpiresIn)> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (plainToken, entity) = CreateTokenEntity(userId);
        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (plainToken, GetRefreshExpiresInSeconds());
    }

    public async Task<ErrorOr<RefreshRotationResult>> ValidateAndRotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActive)
        {
            return DomainErrors.InvalidRefreshToken;
        }

        var user = stored.User;
        if (!user.IsActive)
        {
            return DomainErrors.InvalidRefreshToken;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return DomainErrors.InvalidRefreshToken;
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        var (newPlainToken, newEntity) = CreateTokenEntity(user.Id);
        stored.ReplacedByTokenHash = newEntity.TokenHash;
        dbContext.RefreshTokens.Add(newEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshRotationResult(
            new AuthenticatedUser(
                user.Id,
                user.UserName ?? string.Empty,
                user.FullName,
                roles.ToList(),
                user.MustChangePassword,
                user.SelectedStationId),
            newPlainToken,
            GetRefreshExpiresInSeconds());
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (stored is null || stored.RevokedAtUtc is not null)
        {
            return;
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private (string PlainToken, RefreshToken Entity) CreateTokenEntity(Guid userId)
    {
        var plainToken = GenerateSecureToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plainToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshExpiresInDays())
        };

        return (plainToken, entity);
    }

    private int GetRefreshExpiresInDays() =>
        int.Parse(configuration["Jwt:RefreshTokenExpiresInDays"] ?? "7");

    private int GetRefreshExpiresInSeconds() => GetRefreshExpiresInDays() * 24 * 60 * 60;

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
