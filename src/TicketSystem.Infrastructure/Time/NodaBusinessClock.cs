namespace TicketSystem.Infrastructure.Time;

using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Extensions;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Domain.Common;

public sealed class NodaBusinessClock : IBusinessClock
{
    private readonly IClock _clock;
    private readonly DateTimeZone _zone;

    public NodaBusinessClock(IConfiguration configuration)
    {
        var timeZoneId = configuration["BusinessTimeZone:Id"] ?? BusinessTimeZone.Id;
        _zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId)
                ?? throw new InvalidOperationException($"Unknown time zone '{timeZoneId}'.");
        _clock = SystemClock.Instance;
    }

    public string TimeZoneId => _zone.Id;

    public DateTime UtcNow => _clock.GetCurrentInstant().ToDateTimeUtc();

    public DateOnly Today => ToLocalDate(UtcNow);

    public DateTime ToUtcFromLocal(DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        var local = LocalDateTime.FromDateTime(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified));
        return local.InZoneLeniently(_zone).ToInstant().ToDateTimeUtc();
    }

    public DateTime ToLocalDateTime(DateTime utc)
    {
        var instant = ToInstant(utc);
        return instant.InZone(_zone).ToDateTimeUnspecified();
    }

    public DateTimeOffset ToLocalOffset(DateTime utc) => ToInstant(utc).InZone(_zone).ToDateTimeOffset();

    public DateOnly ToLocalDate(DateTime utc)
    {
        var local = ToInstant(utc).InZone(_zone).Date;
        return new DateOnly(local.Year, local.Month, local.Day);
    }

    public (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForLocalDate(DateOnly localDate)
    {
        var start = new LocalDate(localDate.Year, localDate.Month, localDate.Day);
        var end = start.PlusDays(1);

        return (
            start.AtStartOfDayInZone(_zone).ToInstant().ToDateTimeUtc(),
            end.AtStartOfDayInZone(_zone).ToInstant().ToDateTimeUtc());
    }

    private static Instant ToInstant(DateTime utc) =>
        Instant.FromDateTimeUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
