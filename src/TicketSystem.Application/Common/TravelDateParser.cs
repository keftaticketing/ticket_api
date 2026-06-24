namespace TicketSystem.Application.Common;

using System.Globalization;
using ErrorOr;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;

public static class TravelDateParser
{
    private static readonly string[] LocalDateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.fff",
    ];

    private static readonly string[] OffsetDateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.fffK",
    ];

    /// <summary>
    /// Parses a travel-date query value into an Addis Ababa calendar date.
    /// Values without an explicit offset are treated as Addis local wall time.
    /// </summary>
    public static ErrorOr<DateOnly> ParseLocalDate(string? value, IBusinessClock clock)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainErrors.InvalidTravelDate;
        }

        var normalized = NormalizeQueryDate(value.Trim());

        if (HasTimezone(normalized))
        {
            if (!TryParseWithOffset(normalized, out var withOffset))
            {
                return DomainErrors.InvalidTravelDate;
            }

            return clock.ToLocalDate(withOffset.UtcDateTime);
        }

        if (TryExtractLeadingCalendarDate(normalized, out var leadingDate))
        {
            return leadingDate;
        }

        if (DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        if (DateTime.TryParseExact(
                normalized,
                LocalDateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDateTime)
            || DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out localDateTime))
        {
            return clock.ToLocalDate(clock.ToUtcFromLocal(localDateTime));
        }

        return DomainErrors.InvalidTravelDate;
    }

    public static bool TryParseLocalDate(string? value, IBusinessClock clock, out DateOnly date)
    {
        var parsed = ParseLocalDate(value, clock);
        if (parsed.IsError)
        {
            date = default;
            return false;
        }

        date = parsed.Value;
        return true;
    }

    private static bool TryExtractLeadingCalendarDate(string value, out DateOnly date)
    {
        if (value.Length >= 10
            && value[4] == '-'
            && value[7] == '-'
            && DateOnly.TryParseExact(value.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        date = default;
        return false;
    }

    private static string NormalizeQueryDate(string value)
    {
        if (value.Contains('%', StringComparison.Ordinal))
        {
            value = Uri.UnescapeDataString(value);
        }

        return FixSpaceBeforeOffset(value);
    }

    private static bool TryParseWithOffset(string value, out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
        {
            return true;
        }

        return DateTimeOffset.TryParseExact(
            value,
            OffsetDateTimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    /// <summary>
    /// Query strings treat '+' as space (application/x-www-form-urlencoded).
    /// Produces values like "2026-06-21T08:00:00 03:00" instead of "...+03:00".
    /// </summary>
    private static string FixSpaceBeforeOffset(string value)
    {
        var spaceIndex = value.LastIndexOf(' ');
        if (spaceIndex <= 0 || spaceIndex >= value.Length - 1)
        {
            return value;
        }

        var candidateOffset = value[(spaceIndex + 1)..];

        if (candidateOffset.Length >= 5
            && (candidateOffset[0] == '+' || candidateOffset[0] == '-')
            && char.IsDigit(candidateOffset[1]))
        {
            return string.Concat(value.AsSpan(0, spaceIndex), candidateOffset);
        }

        if (IsUnsignedOffset(candidateOffset))
        {
            return string.Concat(value.AsSpan(0, spaceIndex), "+", candidateOffset);
        }

        return value;
    }

    private static bool HasTimezone(string value)
    {
        if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Contains('+'))
        {
            return true;
        }

        var lastDash = value.LastIndexOf('-');
        return lastDash > 10 && value.Length - lastDash >= 6;
    }

    private static bool IsUnsignedOffset(ReadOnlySpan<char> value) =>
        value.Length == 5
        && char.IsDigit(value[0])
        && char.IsDigit(value[1])
        && value[2] == ':'
        && char.IsDigit(value[3])
        && char.IsDigit(value[4]);
}
