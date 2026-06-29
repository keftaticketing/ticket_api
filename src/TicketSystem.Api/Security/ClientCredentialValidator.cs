namespace TicketSystem.Api.Security;

using System.Security.Cryptography;
using System.Text;
using TicketSystem.Api.Options;

public static class ClientCredentialValidator
{
    public static bool TryValidate(HttpRequest request, params ClientCredentialOptions[] profiles)
    {
        if (!request.Headers.TryGetValue(ClientCredentialHeaders.ClientId, out var clientIdValues)
            || !request.Headers.TryGetValue(ClientCredentialHeaders.ClientKey, out var clientKeyValues))
        {
            return false;
        }

        var clientId = clientIdValues.ToString();
        var clientKey = clientKeyValues.ToString();

        foreach (var profile in profiles)
        {
            if (!profile.IsConfigured)
            {
                continue;
            }

            if (string.Equals(clientId, profile.ClientId, StringComparison.Ordinal)
                && FixedTimeEquals(clientKey, profile.SharedKey))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
