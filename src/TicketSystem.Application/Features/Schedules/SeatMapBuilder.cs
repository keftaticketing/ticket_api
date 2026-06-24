namespace TicketSystem.Application.Features.Schedules;

using TicketSystem.Contracts.Schedules;

public static class SeatMapBuilder
{
    public static SeatMapSummary Build(int totalSeats, IReadOnlyCollection<int> soldSeatNumbers)
    {
        var soldSet = soldSeatNumbers as HashSet<int> ?? soldSeatNumbers.ToHashSet();
        var soldCount = soldSet.Count;

        var seats = Enumerable.Range(1, totalSeats)
            .Select(seatNumber => new SeatStatusResponse(
                seatNumber,
                soldSet.Contains(seatNumber) ? SeatStatuses.Sold : SeatStatuses.Available))
            .ToList();

        var availableCount = totalSeats - soldCount;
        return new SeatMapSummary(seats, soldCount, availableCount, availableCount == 0);
    }

    public static SeatStatusResponse BuildSeat(int seatNumber, IReadOnlyCollection<int> soldSeatNumbers)
    {
        var sold = soldSeatNumbers.Contains(seatNumber);
        return new SeatStatusResponse(seatNumber, sold ? SeatStatuses.Sold : SeatStatuses.Available);
    }
}

public sealed record SeatMapSummary(
    IReadOnlyList<SeatStatusResponse> Seats,
    int SoldSeatCount,
    int AvailableSeatCount,
    bool IsFullySold);
