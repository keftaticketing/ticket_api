namespace TicketSystem.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.Application.Abstractions.Authentication;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public (string Token, int ExpiresIn) CreateToken(
        Guid userId,
        string username,
        string fullName,
        IReadOnlyList<string> roles)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = jwtSection["Issuer"] ?? "TicketSystem";
        var audience = jwtSection["Audience"] ?? "TicketSystem";
        var expiresInMinutes = int.Parse(jwtSection["ExpiresInMinutes"] ?? "480");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, fullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInMinutes * 60);
    }
}
