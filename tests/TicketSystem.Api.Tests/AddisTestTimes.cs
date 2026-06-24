using NodaTime;
using NodaTime.Extensions;
using TicketSystem.Domain.Common;

namespace TicketSystem.Api.Tests;

internal static class AddisTestTimes
{
    private static readonly DateTimeZone Zone = DateTimeZoneProviders.Tzdb[BusinessTimeZone.Id];

    public static DateTime TodayAt(int hour, int minute = 0)
    {
        var today = SystemClock.Instance.GetCurrentInstant().InZone(Zone).Date;
        return today.At(new LocalTime(hour, minute)).ToDateTimeUnspecified();
    }

    public static DateOnly DateOf(DateTime addisLocalDateTime) =>
        new(addisLocalDateTime.Year, addisLocalDateTime.Month, addisLocalDateTime.Day);
}
