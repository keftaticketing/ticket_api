namespace TicketSystem.Application.Abstractions.Time;

/// <summary>
/// Provides time operations anchored to the business region (Addis Ababa, Ethiopia).
/// All UTC values are stored in the database; API-facing values use local wall-clock time.
/// </summary>
public interface IBusinessClock
{
    string TimeZoneId { get; }

    DateTime UtcNow { get; }

    DateOnly Today { get; }

    DateTime ToUtcFromLocal(DateTime localDateTime);

    DateTime ToLocalDateTime(DateTime utc);

    DateTimeOffset ToLocalOffset(DateTime utc);

    DateOnly ToLocalDate(DateTime utc);

    (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForLocalDate(DateOnly localDate);
}
