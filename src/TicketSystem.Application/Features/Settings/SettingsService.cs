namespace TicketSystem.Application.Features.Settings;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Common.Constants;
using TicketSystem.Contracts.Settings;
using TicketSystem.Domain.Entities;

public interface ISettingsService
{
    Task<ErrorOr<PaymentSettingsResponse>> GetPaymentSettingsAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<PaymentSettingsResponse>> UpdatePaymentSettingsAsync(UpdatePaymentSettingsRequest request, CancellationToken cancellationToken = default);
}

public sealed class SettingsService(IApplicationDbContext db) : ISettingsService
{
    public async Task<ErrorOr<PaymentSettingsResponse>> GetPaymentSettingsAsync(CancellationToken cancellationToken = default)
    {
        var setting = await db.AppSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == AppSettingKeys.OnlinePaymentEnabled, cancellationToken);

        var enabled = setting is not null && bool.TryParse(setting.Value, out var parsed) && parsed;
        return new PaymentSettingsResponse(enabled);
    }

    public async Task<ErrorOr<PaymentSettingsResponse>> UpdatePaymentSettingsAsync(
        UpdatePaymentSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var setting = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == AppSettingKeys.OnlinePaymentEnabled, cancellationToken);
        if (setting is null)
        {
            setting = new AppSetting { Key = AppSettingKeys.OnlinePaymentEnabled };
            db.AppSettings.Add(setting);
        }

        setting.Value = request.OnlinePaymentEnabled.ToString().ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return new PaymentSettingsResponse(request.OnlinePaymentEnabled);
    }
}
