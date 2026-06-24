namespace TicketSystem.Api.Controllers;

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketSystem.Api.Options;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Contracts.Sse;

[Route("api/sse")]
public sealed class SseController(
    ISeatEventPublisher seatEvents,
    IScheduleService scheduleService,
    IBusinessClock clock,
    IOptions<SseOptions> sseOptions,
    ILogger<SseController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TimeSpan _heartbeatInterval =
        TimeSpan.FromSeconds(Math.Max(1, sseOptions.Value.HeartbeatSeconds));

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("schedules/{scheduleId:guid}")]
    public Task StreamSchedule(Guid scheduleId, CancellationToken cancellationToken) =>
        StreamScheduleInternalAsync(scheduleId, cancellationToken);

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("routes/{routeId:guid}")]
    public Task StreamRoute(
        Guid routeId,
        [FromQuery] string date,
        CancellationToken cancellationToken) =>
        StreamRouteInternalAsync(routeId, date, cancellationToken);

    private async Task StreamScheduleInternalAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var seatMap = await scheduleService.GetSeatMapAsync(scheduleId, cancellationToken);
        if (seatMap.IsError)
        {
            Response.StatusCode = seatMap.FirstError.Type switch
            {
                ErrorOr.ErrorType.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            HttpContext.RequestAborted);

        await StreamEventsAsync(
            linked.Token,
            new SseConnectedPayload(scheduleId, clock.UtcNow),
            seatEvents.SubscribeScheduleAsync(scheduleId, linked.Token));
    }

    private async Task StreamRouteInternalAsync(
        Guid routeId,
        string date,
        CancellationToken cancellationToken)
    {
        if (!TravelDateParser.TryParseLocalDate(date, clock, out var travelDate))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var schedules = await scheduleService.GetAllAsync(routeId, travelDate, cancellationToken);
        if (schedules.IsError)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            HttpContext.RequestAborted);

        await StreamEventsAsync(
            linked.Token,
            new SseRouteConnectedPayload(routeId, travelDate, clock.UtcNow),
            seatEvents.SubscribeRouteAsync(routeId, travelDate, linked.Token));
    }

    private async Task StreamEventsAsync(
        CancellationToken cancellationToken,
        object connectedPayload,
        IAsyncEnumerable<SeatStreamEvent> subscription)
    {
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Pragma = "no-cache";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

        try
        {
            await Response.StartAsync(cancellationToken);
            await WriteEventAsync(SeatSseEventNames.Connected, connectedPayload, cancellationToken);

            await foreach (var evt in WithHeartbeats(subscription, cancellationToken))
            {
                await WriteEventAsync(evt.EventType, evt.Payload, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected or request aborted.
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "SSE client disconnected while writing.");
        }
    }

    private async IAsyncEnumerable<SeatStreamEvent> WithHeartbeats(
        IAsyncEnumerable<SeatStreamEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var moveNextTask = enumerator.MoveNextAsync().AsTask();

            while (!moveNextTask.IsCompleted)
            {
                var completed = await Task.WhenAny(
                    moveNextTask,
                    Task.Delay(_heartbeatInterval, cancellationToken));

                if (completed != moveNextTask)
                {
                    yield return new SeatStreamEvent(
                        SeatSseEventNames.Heartbeat,
                        new SseHeartbeatPayload(clock.UtcNow));
                }
            }

            if (!await moveNextTask)
            {
                yield break;
            }

            yield return enumerator.Current;
        }
    }

    private async Task WriteEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await Response.WriteAsync($"event: {eventType}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
