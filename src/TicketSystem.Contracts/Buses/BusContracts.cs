namespace TicketSystem.Contracts.Buses;

public sealed record CreateBusRequest(
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount,
    Guid? AssociationId = null,
    Guid? BusLevelId = null,
    Guid? BusTypeId = null);

public sealed record UpdateBusRequest(
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount,
    bool IsActive,
    Guid? AssociationId = null,
    Guid? BusLevelId = null,
    Guid? BusTypeId = null);

public sealed record BusAssociationResponse(
    Guid Id,
    string Name,
    string Code);

public sealed record BusLevelReferenceResponse(
    Guid Id,
    string Code,
    string Name,
    int Rank);

public sealed record BusTypeReferenceResponse(
    Guid Id,
    string Code,
    string Name);

public sealed record BusResponse(
    Guid Id,
    string OwnerName,
    string OwnerPhone,
    string DelegatePhone,
    string SideNumber,
    string PlateNumber,
    int SeatCount,
    BusAssociationResponse Association,
    BusLevelReferenceResponse BusLevel,
    BusTypeReferenceResponse BusType,
    bool IsActive,
    DateTime CreatedAt);
