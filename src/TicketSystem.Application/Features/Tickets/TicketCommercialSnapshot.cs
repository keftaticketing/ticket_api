namespace TicketSystem.Application.Features.Tickets;

using TicketSystem.Domain.Entities;

public static class TicketCommercialSnapshot
{
    public static void ApplyFromSchedule(Ticket ticket, Schedule schedule)
    {
        var route = schedule.Route;

        ticket.FromCityId = route.FromCityId;
        ticket.FromCityName = route.FromCity.Name;
        ticket.FromStationId = route.FromStationId;
        ticket.FromStationName = route.FromStation.Name;
        ticket.ToCityId = route.ToCityId;
        ticket.ToCityName = route.ToCity.Name;
        ticket.ToStationId = route.ToStationId;
        ticket.ToStationName = route.ToStation.Name;
        ticket.AssociationId = schedule.AssociationId;
        ticket.AssociationName = schedule.Association.Name;
        ticket.BusLevelId = schedule.BusLevelId;
        ticket.BusLevelName = schedule.BusLevel.Name;
        ticket.BusTypeId = schedule.BusTypeId;
        ticket.BusTypeName = schedule.BusType.Name;
        ticket.TariffId = schedule.TariffId;
        ticket.Price = schedule.ResolvedTicketPrice;
        ticket.DistanceKm = schedule.ResolvedDistanceKm;
        ticket.RatePerKm = schedule.ResolvedRatePerKm;
    }
}
