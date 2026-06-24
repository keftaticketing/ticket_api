namespace TicketSystem.Contracts.Buses;

public sealed record CreateBusRequest(
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount);

public sealed record UpdateBusRequest(
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount,
    bool IsActive);

public sealed record BusResponse(
    Guid Id,
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount,
    bool IsActive,
    DateTime CreatedAt);
