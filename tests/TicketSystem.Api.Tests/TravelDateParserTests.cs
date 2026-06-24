using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Domain.Common;
using TicketSystem.Infrastructure.Time;

namespace TicketSystem.Api.Tests;

public sealed class TravelDateParserTests
{
    private static readonly IBusinessClock Clock = new NodaBusinessClock(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BusinessTimeZone:Id"] = BusinessTimeZone.Id })
            .Build());

    private static readonly DateOnly Expected = new(2026, 6, 21);

    [Theory]
    [InlineData("2026-06-21T08:00:00+03:00")]
    [InlineData("2026-06-21T08:00:00 03:00")]
    [InlineData("2026-06-21T08%3A00%3A00%2B03%3A00")]
    [InlineData("2026-06-21T08:00:00")]
    [InlineData("2026-06-21")]
    public void ParseLocalDate_AcceptsCommonQueryFormats(string input)
    {
        var result = TravelDateParser.ParseLocalDate(input, Clock);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Expected);
    }

    [Fact]
    public void ParseLocalDate_UsesAddisCalendarDayForUtcMidnightCrossing()
    {
        var result = TravelDateParser.ParseLocalDate("2026-06-20T22:00:00Z", Clock);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Expected);
    }
}
