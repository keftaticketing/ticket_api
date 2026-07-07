namespace TicketSystem.Application.Features.Schedules;

using TicketSystem.Domain.Entities;

public static class ScheduleOptionOrdering
{
    public static void ApplyClassificationSnapshot(Schedule schedule, Bus bus, DateOnly departureDate)
    {
        schedule.DepartureDate = departureDate;
        schedule.AssociationId = bus.AssociationId;
        schedule.BusLevelId = bus.BusLevelId;
        schedule.BusTypeId = bus.BusTypeId;
    }

    public static IQueryable<Schedule> OrderForOptionDisplay(this IQueryable<Schedule> query) =>
        query
            .OrderBy(x => x.BusLevel.Rank)
            .ThenBy(x => x.BusType.Code)
            .ThenBy(x => x.SequenceNumber)
            .ThenBy(x => x.DepartureAt);
}
