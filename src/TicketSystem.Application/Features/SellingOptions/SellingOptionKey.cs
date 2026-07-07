namespace TicketSystem.Application.Features.SellingOptions;

using System.Globalization;
using ErrorOr;
using TicketSystem.Application.Errors;

public static class SellingOptionKey
{
    public static string Build(
        Guid routeId,
        Guid associationId,
        Guid busLevelId,
        Guid busTypeId,
        DateOnly date) =>
        $"{routeId}|{associationId}|{busLevelId}|{busTypeId}|{date:yyyy-MM-dd}";

    public static ErrorOr<(Guid RouteId, Guid AssociationId, Guid BusLevelId, Guid BusTypeId, DateOnly Date)> TryParse(
        string optionKey)
    {
        if (string.IsNullOrWhiteSpace(optionKey))
        {
            return DomainErrors.InvalidSellingOptionKey;
        }

        var parts = optionKey.Split('|');
        if (parts.Length != 5
            || !Guid.TryParse(parts[0], out var routeId)
            || !Guid.TryParse(parts[1], out var associationId)
            || !Guid.TryParse(parts[2], out var busLevelId)
            || !Guid.TryParse(parts[3], out var busTypeId)
            || !DateOnly.TryParseExact(parts[4], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return DomainErrors.InvalidSellingOptionKey;
        }

        return (routeId, associationId, busLevelId, busTypeId, date);
    }
}
