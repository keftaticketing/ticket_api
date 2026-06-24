namespace TicketSystem.Infrastructure.Realtime;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Contracts.Sse;

public sealed class SeatEventHub : ISeatEventPublisher
{
    private readonly ConcurrentDictionary<Guid, SubscriptionGroup> _scheduleGroups = new();
    private readonly ConcurrentDictionary<RouteDateKey, SubscriptionGroup> _routeGroups = new();

    public async IAsyncEnumerable<SeatStreamEvent> SubscribeScheduleAsync(
        Guid scheduleId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var evt in SubscribeAsync(
                           _scheduleGroups,
                           scheduleId,
                           cancellationToken))
        {
            yield return evt;
        }
    }

    public async IAsyncEnumerable<SeatStreamEvent> SubscribeRouteAsync(
        Guid routeId,
        DateOnly date,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var evt in SubscribeAsync(
                           _routeGroups,
                           new RouteDateKey(routeId, date),
                           cancellationToken))
        {
            yield return evt;
        }
    }

    public void PublishSeatSold(
        Guid scheduleId,
        Guid routeId,
        DateOnly travelDate,
        int seatNumber,
        Guid ticketId,
        int soldSeatCount,
        int availableSeatCount,
        bool isFullySold)
    {
        var payload = new SseSeatSoldPayload(
            scheduleId,
            seatNumber,
            ticketId,
            soldSeatCount,
            availableSeatCount,
            isFullySold);

        var evt = new SeatStreamEvent(SeatSseEventNames.SeatSold, payload);
        Publish(_scheduleGroups, scheduleId, evt);
        Publish(_routeGroups, new RouteDateKey(routeId, travelDate), evt);
    }

    public void PublishScheduleUpdated(
        Guid scheduleId,
        Guid routeId,
        DateOnly travelDate,
        string status)
    {
        var payload = new SseScheduleUpdatedPayload(scheduleId, status);
        var evt = new SeatStreamEvent(SeatSseEventNames.ScheduleUpdated, payload);
        Publish(_scheduleGroups, scheduleId, evt);
        Publish(_routeGroups, new RouteDateKey(routeId, travelDate), evt);
    }

    private static async IAsyncEnumerable<SeatStreamEvent> SubscribeAsync<TKey>(
        ConcurrentDictionary<TKey, SubscriptionGroup> groups,
        TKey key,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TKey : notnull
    {
        var channel = Channel.CreateUnbounded<SeatStreamEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false });

        var group = groups.GetOrAdd(key, _ => new SubscriptionGroup());
        var subscriptionId = group.Add(channel.Writer);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            group.Remove(subscriptionId);
            channel.Writer.TryComplete();
            if (group.IsEmpty)
            {
                groups.TryRemove(key, out _);
            }
        }
    }

    private static void Publish<TKey>(
        ConcurrentDictionary<TKey, SubscriptionGroup> groups,
        TKey key,
        SeatStreamEvent evt)
        where TKey : notnull
    {
        if (groups.TryGetValue(key, out var group))
        {
            group.Publish(evt);
        }
    }

    private sealed class SubscriptionGroup
    {
        private readonly ConcurrentDictionary<Guid, ChannelWriter<SeatStreamEvent>> _writers = new();

        public bool IsEmpty => _writers.IsEmpty;

        public Guid Add(ChannelWriter<SeatStreamEvent> writer)
        {
            var id = Guid.NewGuid();
            _writers[id] = writer;
            return id;
        }

        public void Remove(Guid id) => _writers.TryRemove(id, out _);

        public void Publish(SeatStreamEvent evt)
        {
            foreach (var pair in _writers)
            {
                if (!pair.Value.TryWrite(evt))
                {
                    _writers.TryRemove(pair.Key, out _);
                }
            }
        }
    }

    private readonly record struct RouteDateKey(Guid RouteId, DateOnly Date);
}
