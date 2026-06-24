namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Settings;
using TicketSystem.Contracts.Settings;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/settings/payments")]
public sealed class PaymentSettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaymentSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetPaymentSettingsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut]
    public async Task<ActionResult<PaymentSettingsResponse>> Update(
        [FromBody] UpdatePaymentSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdatePaymentSettingsAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}
