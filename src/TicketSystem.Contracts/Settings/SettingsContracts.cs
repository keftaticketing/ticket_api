namespace TicketSystem.Contracts.Settings;

public sealed record PaymentSettingsResponse(bool OnlinePaymentEnabled);

public sealed record UpdatePaymentSettingsRequest(bool OnlinePaymentEnabled);
