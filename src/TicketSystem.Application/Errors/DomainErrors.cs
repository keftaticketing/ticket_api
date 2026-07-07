namespace TicketSystem.Application.Errors;

using ErrorOr;

public static class DomainErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

    public static Error PasswordChangeRequired =>
        Error.Forbidden("Auth.PasswordChangeRequired", "You must change your password before continuing.");

    public static Error CurrentPasswordIncorrect =>
        Error.Validation("Auth.CurrentPasswordIncorrect", "Current password is incorrect.");

    public static Error PasswordChangeFailed(string description) =>
        Error.Validation("Auth.PasswordChangeFailed", description);

    public static Error UserNotFound =>
        Error.NotFound("User.NotFound", "User not found.");

    public static Error DuplicateUsername =>
        Error.Conflict("User.DuplicateUsername", "A user with this username already exists.");

    public static Error StationNotFound =>
        Error.NotFound("Station.NotFound", "Station not found.");

    public static Error StationAssignmentNotFound =>
        Error.NotFound("StationAssignment.NotFound", "Station assignment not found.");

    public static Error StationAssignmentAlreadyActive =>
        Error.Conflict("StationAssignment.AlreadyActive", "This user already has an active assignment for the selected station.");

    public static Error SelectedStationNotAssigned =>
        Error.Validation("StationAssignment.SelectedStationNotAssigned", "The selected station is not an active assignment for this user.");

    public static Error StationCityMismatch =>
        Error.Validation("Station.CityMismatch", "The selected station does not belong to the specified city.");

    public static Error DefaultStationNotFound =>
        Error.NotFound("Station.DefaultNotFound", "No default station is configured for the city.");

    public static Error UsernameRequired =>
        Error.Validation("User.UsernameRequired", "Username is required.");

    public static Error FullNameRequired =>
        Error.Validation("User.FullNameRequired", "Full name is required.");

    public static Error AssociationNotFound =>
        Error.NotFound("Association.NotFound", "Association not found.");

    public static Error BusLevelNotFound =>
        Error.NotFound("BusLevel.NotFound", "Bus level not found.");

    public static Error BusTypeNotFound =>
        Error.NotFound("BusType.NotFound", "Bus type not found.");

    public static Error BusNotFound =>
        Error.NotFound("Bus.NotFound", "Bus not found.");

    public static Error DuplicatePlateNumber =>
        Error.Conflict("Bus.DuplicatePlateNumber", "A bus with this plate number already exists.");

    public static Error DuplicateSideNumber =>
        Error.Conflict("Bus.DuplicateSideNumber", "A bus with this side number already exists.");

    public static Error InvalidSeatCount =>
        Error.Validation("Bus.InvalidSeatCount", "Seat count must be at least 1.");

    public static Error RouteNotFound =>
        Error.NotFound("Route.NotFound", "Route not found.");

    public static Error RouteInactive =>
        Error.NotFound("Route.Inactive", "Route not found or inactive.");

    public static Error DuplicateRoute =>
        Error.Conflict("Route.Duplicate", "This route already exists.");

    public static Error InvalidDistance =>
        Error.Validation("Route.InvalidDistance", "Distance must be greater than zero.");

    public static Error TariffNotFound =>
        Error.NotFound("Tariff.NotFound", "No active tariff configured for the selected bus classification.");

    public static Error InvalidRatePerKm =>
        Error.Validation("Tariff.InvalidRate", "Rate per km must be greater than zero.");

    public static Error ScheduleNotFound =>
        Error.NotFound("Schedule.NotFound", "Schedule not found.");

    public static Error ScheduleCancelled =>
        Error.Validation("Schedule.Cancelled", "Schedule is cancelled.");

    public static Error InvalidSequenceNumber =>
        Error.Validation("Schedule.InvalidSequence", "Sequence number must be at least 1.");

    public static Error InvalidScheduleStatus =>
        Error.Validation("Schedule.InvalidStatus", "Invalid schedule status.");

    public static Error BusAlreadyScheduled =>
        Error.Conflict("Schedule.BusAlreadyScheduled", "This bus already has a schedule on the selected day.");

    public static Error DuplicateSequence =>
        Error.Conflict(
            "Schedule.DuplicateSequence",
            "Sequence number already used for this route, day, and selling option.");

    public static Error BusInactive =>
        Error.NotFound("Bus.Inactive", "Bus not found or inactive.");

    public static Error TicketNotFound =>
        Error.NotFound("Ticket.NotFound", "Ticket not found.");

    public static Error PassengerNameRequired =>
        Error.Validation("Ticket.PassengerNameRequired", "Passenger name is required.");

    public static Error PassengerPhoneRequired =>
        Error.Validation("Ticket.PassengerPhoneRequired", "Passenger phone is required.");

    public static Error InvalidScheduleForSale =>
        Error.Validation("Ticket.InvalidScheduleStatus", "Tickets cannot be sold for this schedule status.");

    public static Error InvalidSeatNumber(int maxSeats) =>
        Error.Validation("Ticket.InvalidSeatNumber", $"Seat number must be between 1 and {maxSeats}.");

    public static Error SeatAlreadySold =>
        Error.Conflict("Ticket.SeatAlreadySold", "Seat is already sold.");

    public static Error TicketerRequired =>
        Error.Forbidden("Ticket.TicketerRequired", "Only active ticketers can sell tickets.");

    public static Error SellingStationNotAssigned =>
        Error.Validation("Ticket.SellingStationNotAssigned", "The ticketer has no active station assignment.");

    public static Error SellingStationNotSelected =>
        Error.Validation("Ticket.SellingStationNotSelected", "Select a selling station before selling tickets.");

    public static Error ScheduleOriginStationMismatch =>
        Error.Validation("Ticket.ScheduleOriginStationMismatch", "Tickets can only be sold for schedules departing from the ticketer's assigned station.");

    public static Error FromStationScopeMismatch =>
        Error.Validation("Station.FromStationScopeMismatch", "The requested origin station is outside the ticketer's selling scope.");

    public static Error CityNotFound =>
        Error.NotFound("City.NotFound", "City not found.");

    public static Error CityInactive =>
        Error.NotFound("City.Inactive", "City not found or inactive.");

    public static Error DuplicateCity =>
        Error.Conflict("City.Duplicate", "A city with this name already exists.");

    public static Error CityNameRequired =>
        Error.Validation("City.NameRequired", "City name is required.");

    public static Error SameOriginDestination =>
        Error.Validation("Route.SameOriginDestination", "Origin and destination must be different cities.");

    public static Error AddisAbabaNotFound =>
        Error.NotFound("City.AddisAbabaNotFound", "Addis Ababa is not configured as a city.");

    public static Error InvalidCityDistance =>
        Error.Validation("City.InvalidDistance", "Distance from Addis Ababa must be zero for Addis Ababa and greater than zero for other cities.");

    public static Error DestinationCityRequired =>
        Error.Validation("Route.DestinationRequired", "Destination city is required.");

    public static Error InvalidTravelDate =>
        Error.Validation("Route.InvalidTravelDate", "Invalid travel date. Use ISO format, e.g. 2026-06-21T08:00:00+03:00.");

    public static Error SalesPartyNotFound =>
        Error.NotFound("SalesParty.NotFound", "Sales party not found.");

    public static Error SalesPartyNameRequired =>
        Error.Validation("SalesParty.NameRequired", "Sales party name and code are required.");

    public static Error DuplicateSalesPartyCode =>
        Error.Conflict("SalesParty.DuplicateCode", "A sales party with this code already exists.");

    public static Error InvalidSalesPartySource =>
        Error.Validation("SalesParty.InvalidSource", "Invalid sales party source.");

    public static Error InvalidSalesPartyAllocationType =>
        Error.Validation("SalesParty.InvalidAllocationType", "Invalid sales party allocation type.");

    public static Error InvalidSalesPartyAmount =>
        Error.Validation("SalesParty.InvalidAmount", "Amount per seat must be greater than zero for fixed-amount parties.");

    public static Error BusOwnerRemainderMustHaveZeroAmount =>
        Error.Validation("SalesParty.BusOwnerRemainderAmount", "Bus owner remainder party must have zero configured amount.");

    public static Error DuplicateBusOwnerRemainderParty =>
        Error.Conflict("SalesParty.DuplicateBusOwnerRemainder", "Only one active bus owner remainder party is allowed.");

    public static Error SalesPartyConfigurationMissing =>
        Error.Validation("SalesParty.NotConfigured", "Sales party configuration is missing or invalid.");

    public static Error BusOwnerDeductionExceedsFare =>
        Error.Validation("SalesParty.DeductionExceedsFare", "Configured stakeholder deductions exceed the ticket fare.");

    public static Error InvalidReportDateRange =>
        Error.Validation("Reports.InvalidDateRange", "The 'from' date must be on or before the 'to' date.");

    public static Error ReportDateRangeTooLarge =>
        Error.Validation("Reports.DateRangeTooLarge", "Date range cannot exceed 366 days.");
}
