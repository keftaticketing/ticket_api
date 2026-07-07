using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TicketSystem.Contracts.Cities;
using TicketSystem.Contracts.Auth;
using TicketSystem.Contracts.Buses;
using TicketSystem.Contracts.Routes;
using TicketSystem.Contracts.Schedules;
using TicketSystem.Contracts.Settings;
using TicketSystem.Contracts.Tariffs;
using TicketSystem.Contracts.Tickets;
using TicketSystem.Contracts.Users;

namespace TicketSystem.Api.Tests;

[Collection("Api")]
public sealed class AuthEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task Login_WithAdminCredentials_ReturnsToken()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", TestDataSeeder.AdminWorkingPassword));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Role.Should().Be("Admin");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithTicketerCredentials_ReturnsToken()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ticketer", TestDataSeeder.TicketerWorkingPassword));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Role.Should().Be("Ticketer");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "wrong"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin", TestDataSeeder.AdminWorkingPassword));
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var refresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(body!.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>();
        refreshed!.AccessToken.Should().NotBe(body.AccessToken);
        refreshed.RefreshToken.Should().NotBe(body.RefreshToken);
    }
}

[Collection("Api")]
public sealed class UserEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task CreateTicketer_AsAdmin_ReturnsCreatedUser()
    {
        var client = AdminClient();
        var response = await client.PostAsJsonAsync(
            "/api/users",
            new CreateTicketerRequest("counter2", "Counter Two", "TempPass123!", "counter2@local.test"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body!.Username.Should().Be("counter2");
        body.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task ListUsers_AsAdmin_ReturnsUsers()
    {
        var client = AdminClient();
        var response = await client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>();
        body.Should().NotBeEmpty();
    }
}

[Collection("Api")]
public sealed class BusEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task CreateBus_AsAdmin_ReturnsCreated()
    {
        var client = AdminClient();
        var request = new CreateBusRequest("Owner", "0911000000", "0911000001", "S-100", "AA-12345", 45);
        var response = await client.PostAsJsonAsync("/api/buses", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BusResponse>();
        body!.PlateNumber.Should().Be("AA-12345");
    }

    [Fact]
    public async Task GetBuses_AsAdmin_ReturnsList()
    {
        var client = AdminClient();
        await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-101", "AA-12346", 40));
        var response = await client.GetAsync("/api/buses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<BusResponse>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBusById_WhenExists_ReturnsBus()
    {
        var client = AdminClient();
        var created = await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-102", "AA-12347", 40));
        var bus = await created.Content.ReadFromJsonAsync<BusResponse>();
        var response = await client.GetAsync($"/api/buses/{bus!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBusById_WhenMissing_ReturnsNotFound()
    {
        var client = AdminClient();
        var response = await client.GetAsync($"/api/buses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBus_WhenExists_ReturnsUpdatedBus()
    {
        var client = AdminClient();
        var created = await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-103", "AA-12348", 40));
        var bus = await created.Content.ReadFromJsonAsync<BusResponse>();
        var response = await client.PutAsJsonAsync($"/api/buses/{bus!.Id}",
            new UpdateBusRequest("New Owner", "0911000000", "0911000001", "S-103", "AA-12348", 42, true));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<BusResponse>();
        updated!.OwnerName.Should().Be("New Owner");
        updated.SeatCount.Should().Be(42);
    }

    [Fact]
    public async Task CreateBus_AsTicketer_ReturnsForbidden()
    {
        var client = TicketerClient();
        var response = await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-104", "AA-12349", 40));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBus_WithDuplicatePlate_ReturnsConflict()
    {
        var client = AdminClient();
        await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-105", "AA-DUP01", 40));
        var response = await client.PostAsJsonAsync("/api/buses", new CreateBusRequest("Owner", "0911000000", "0911000001", "S-106", "AA-DUP01", 40));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

[Collection("Api")]
public sealed class CityEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task CreateCity_AsAdmin_ReturnsCreated()
    {
        var client = AdminClient();
        var response = await client.PostAsJsonAsync("/api/cities", new CreateCityRequest("Shashamane", 250));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CityResponse>();
        body!.Name.Should().Be("Shashamane");
        body.DistanceFromAddisKm.Should().Be(250);
    }

    [Fact]
    public async Task GetCities_AsAdmin_IncludesSeededAddisAbaba()
    {
        var response = await AdminClient().GetAsync("/api/cities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CityResponse>>();
        body.Should().Contain(x => x.Name == "Addis Ababa" && x.DistanceFromAddisKm == 0);
    }

    [Fact]
    public async Task CreateCity_WithDuplicateName_ReturnsConflict()
    {
        var client = AdminClient();
        await client.PostAsJsonAsync("/api/cities", new CreateCityRequest("Jimma", 346));
        var response = await client.PostAsJsonAsync("/api/cities", new CreateCityRequest("Jimma", 346));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCity_AsTicketer_ReturnsForbidden()
    {
        var client = TicketerClient();
        var response = await client.PostAsJsonAsync("/api/cities", new CreateCityRequest("Hawassa", 275));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDestinations_AsTicketer_ExcludesAddisAbaba()
    {
        await EnsureCityAsync("Addis Ababa");
        await EnsureCityAsync("Bahir Dar");
        var response = await TicketerClient().GetAsync("/api/cities/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CityResponse>>();
        body.Should().Contain(x => x.Name == "Bahir Dar");
        body.Should().NotContain(x => x.Name == "Addis Ababa");
    }

    [Fact]
    public async Task SearchCities_ByPartialName_ReturnsDestinationMatches()
    {
        await EnsureCityAsync("Adama");
        await EnsureCityAsync("Jimma");
        var response = await TicketerClient().GetAsync("/api/cities/search?q=ada");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CityResponse>>();
        body.Should().Contain(x => x.Name == "Adama");
        body.Should().NotContain(x => x.Name == "Jimma");
    }
}

[Collection("Api")]
public sealed class RouteEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task CreateRoute_AsAdmin_ReturnsCreated()
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync("Jimma");
        var response = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RouteResponse>();
        body!.FromCity.Should().Be("Addis Ababa");
        body.ToCity.Should().Be("Jimma");
        body.DistanceKm.Should().Be(346);
    }

    [Fact]
    public async Task GetRoutes_ReturnsList()
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync("Hawassa");
        await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        var response = await client.GetAsync("/api/routes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<RouteResponse>>();
        body.Should().NotBeEmpty();
        body!.Should().OnlyContain(x => x.FromCity == "Addis Ababa");
    }

    [Fact]
    public async Task GetRouteById_WhenExists_ReturnsRoute()
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync("Bahir Dar");
        var created = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        var route = await created.Content.ReadFromJsonAsync<RouteResponse>();
        var response = await client.GetAsync($"/api/routes/{route!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRouteById_WhenMissing_ReturnsNotFound()
    {
        var client = AdminClient();
        var response = await client.GetAsync($"/api/routes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoute_WhenExists_ReturnsUpdatedRoute()
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var direDawaId = await EnsureCityAsync("Dire Dawa");
        var gondarId = await EnsureCityAsync("Gondar");
        var created = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(direDawaId));
        var route = await created.Content.ReadFromJsonAsync<RouteResponse>();
        var response = await client.PutAsJsonAsync($"/api/routes/{route!.Id}",
            new UpdateRouteRequest(gondarId, true));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RouteResponse>();
        updated!.ToCity.Should().Be("Gondar");
        updated.DistanceKm.Should().Be(748);
    }

    [Fact]
    public async Task CreateRoute_WithDuplicatePair_ReturnsConflict()
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync("Gondar");
        await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        var response = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SearchRoutes_ByDestination_AsTicketer_ReturnsMatch()
    {
        var (routeId, _) = await SeedRouteAndBusAsync(to: "Jimma");
        var toCityId = await EnsureCityAsync("Jimma");
        var response = await TicketerClient().GetAsync($"/api/routes?toCityId={toCityId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<RouteResponse>>();
        body.Should().ContainSingle(x => x.Id == routeId);
    }

    [Fact]
    public async Task GetRoutes_WithForbiddenFromStationId_AsTicketer_ReturnsBadRequest()
    {
        await SeedRouteAndBusAsync(to: "Jimma");
        var adamaStationId = await GetDefaultStationIdForCityAsync("Adama");
        var response = await TicketerClient().GetAsync($"/api/routes?fromStationId={adamaStationId}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRouteSeatMaps_ByDestination_AsTicketer_ReturnsSequencedBusSeats()
    {
        var (_, busId) = await SeedRouteAndBusAsync("AA-30001", to: "Jimma");
        var jimmaCityId = await EnsureCityAsync("Jimma");
        var admin = AdminClient();
        var departure = AddisTestTimes.TodayAt(6);
        var routeId = (await (await admin.GetAsync($"/api/routes?toCityId={jimmaCityId}"))
            .Content.ReadFromJsonAsync<List<RouteResponse>>())!.Single().Id;
        await admin.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var response = await TicketerClient().GetAsync(
            $"/api/routes/seats?destinationCityId={jimmaCityId}&date={AddisTestTimes.DateOf(departure):yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RouteSeatMapsResponse>();
        body!.ToCity.Should().Be("Jimma");
        body.DistanceKm.Should().Be(346);
        body.Schedules.Should().ContainSingle();
        body.Schedules[0].SequenceNumber.Should().Be(1);
        body.Schedules[0].AvailableSeatCount.Should().Be(45);
        body.Schedules[0].SoldSeatCount.Should().Be(0);
        body.Schedules[0].IsFullySold.Should().BeFalse();
        body.Schedules[0].Seats.First(x => x.SeatNumber == 1).Status.Should().Be(SeatStatuses.Available);
    }

    [Fact]
    public async Task GetRouteSeatMaps_ByDestination_WithMobileTravelDateTime_ReturnsSchedules()
    {
        var (_, busId) = await SeedRouteAndBusAsync("AA-30002", to: "Jimma");
        var jimmaCityId = await EnsureCityAsync("Jimma");
        var admin = AdminClient();
        var departure = AddisTestTimes.TodayAt(6);
        var routeId = (await (await admin.GetAsync($"/api/routes?toCityId={jimmaCityId}"))
            .Content.ReadFromJsonAsync<List<RouteResponse>>())!.Single().Id;
        await admin.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));

        var travelDate = AddisTestTimes.DateOf(departure).ToString("yyyy-MM-dd");
        var response = await TicketerClient().GetAsync(
            $"/api/routes/seats?destinationCityId={jimmaCityId}&date={travelDate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RouteSeatMapsResponse>();
        body!.Schedules.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRouteSeatMaps_ByDestination_WithIsoDateTimeOffset_ReturnsSchedules()
    {
        var (_, busId) = await SeedRouteAndBusAsync("AA-30002", to: "Jimma");
        var jimmaCityId = await EnsureCityAsync("Jimma");
        var admin = AdminClient();
        var departure = AddisTestTimes.TodayAt(6);
        var routeId = (await (await admin.GetAsync($"/api/routes?toCityId={jimmaCityId}"))
            .Content.ReadFromJsonAsync<List<RouteResponse>>())!.Single().Id;
        await admin.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));

        var travelDate = AddisTestTimes.DateOf(departure).ToString("yyyy-MM-dd");
        var encodedDate = Uri.EscapeDataString($"{travelDate}T08:00:00+03:00");
        var response = await TicketerClient().GetAsync(
            $"/api/routes/seats?destinationCityId={jimmaCityId}&date={encodedDate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RouteSeatMapsResponse>();
        body!.Schedules.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRouteSeatMaps_ByDestination_WithSpaceInsteadOfPlusOffset_ReturnsSchedules()
    {
        var (_, busId) = await SeedRouteAndBusAsync("AA-30003", to: "Jimma");
        var jimmaCityId = await EnsureCityAsync("Jimma");
        var admin = AdminClient();
        var departure = AddisTestTimes.TodayAt(6);
        var routeId = (await (await admin.GetAsync($"/api/routes?toCityId={jimmaCityId}"))
            .Content.ReadFromJsonAsync<List<RouteResponse>>())!.Single().Id;
        await admin.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));

        var travelDate = AddisTestTimes.DateOf(departure).ToString("yyyy-MM-dd");
        var response = await TicketerClient().GetAsync(
            $"/api/routes/seats?destinationCityId={jimmaCityId}&date={travelDate}T08%3A00%3A00%2003%3A00");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RouteSeatMapsResponse>();
        body!.Schedules.Should().ContainSingle();
    }
}

[Collection("Api")]
public sealed class TariffEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task GetActiveTariff_ReturnsSeededTariff()
    {
        var client = TicketerClient();
        var response = await client.GetAsync("/api/tariffs/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TariffResponse>();
        body!.RatePerKm.Should().Be(2.50m);
        body.Currency.Should().Be("ETB");
    }

    [Fact]
    public async Task SetActiveTariff_AsAdmin_UpdatesRate()
    {
        var client = AdminClient();
        var response = await client.PutAsJsonAsync("/api/tariffs", new SetTariffRequest(3.00m));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TariffResponse>();
        body!.RatePerKm.Should().Be(3.00m);
    }

    [Fact]
    public async Task GetTariffHistory_ReturnsEntries()
    {
        var client = AdminClient();
        await client.PutAsJsonAsync("/api/tariffs", new SetTariffRequest(3.25m));
        var response = await client.GetAsync("/api/tariffs/history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<TariffResponse>>();
        body!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SetActiveTariff_AsTicketer_ReturnsForbidden()
    {
        var client = TicketerClient();
        var response = await client.PutAsJsonAsync("/api/tariffs", new SetTariffRequest(4.00m));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

[Collection("Api")]
public sealed class ScheduleEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task CreateSchedule_AsAdmin_ReturnsCreated()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync();
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(6);
        var response = await client.PostAsJsonAsync("/api/schedules",
            new CreateScheduleRequest(routeId, busId, departure, 1));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ScheduleResponse>();
        body!.SequenceNumber.Should().Be(1);
        body.TicketPrice.Should().Be(346 * 2.50m);
    }

    [Fact]
    public async Task GetSchedules_WithFilters_ReturnsList()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync("AA-20001", to: "Jimma");
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(7);
        await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var response = await client.GetAsync($"/api/schedules?routeId={routeId}&date={AddisTestTimes.DateOf(departure):yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ScheduleResponse>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableSchedules_ReturnsScheduledBusesBySequence()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync("AA-20002", to: "Jimma");
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(8);
        await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var ticketer = TicketerClient();
        var response = await ticketer.GetAsync($"/api/schedules/available?routeId={routeId}&date={AddisTestTimes.DateOf(departure):yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ScheduleResponse>>();
        body!.Single().AvailableSeatCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAvailableSchedules_WhenRouteOutsideSellingScope_ReturnsBadRequest()
    {
        var (routeId, _) = await SeedRouteAndBusAsync("AA-20020", to: "Jimma");
        var adamaStationId = await GetDefaultStationIdForCityAsync("Adama");
        var admin = AdminClient();

        await admin.PostAsJsonAsync(
            $"/api/users/{TestDataSeeder.TicketerId}/station-assignments",
            new CreateUserStationAssignmentRequest(adamaStationId));

        var ticketer = TicketerClient();
        await ticketer.PutAsJsonAsync(
            "/api/auth/me/selected-station",
            new SetSelectedStationRequest(adamaStationId));

        var response = await ticketer.GetAsync(
            $"/api/schedules/available?routeId={routeId}&date={AddisTestTimes.DateOf(AddisTestTimes.TodayAt(8)):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSchedule_AsAdmin_ReturnsUpdatedSchedule()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync("AA-20003", to: "Jimma");
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(9);
        var created = await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var schedule = await created.Content.ReadFromJsonAsync<ScheduleResponse>();
        var newDeparture = departure.AddHours(1);
        var response = await client.PutAsJsonAsync($"/api/schedules/{schedule!.Id}",
            new UpdateScheduleRequest(newDeparture, 1, "Boarding"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ScheduleResponse>();
        updated!.Status.Should().Be("Boarding");
    }

    [Fact]
    public async Task GetSeatMap_ReturnsSeatsOneToN()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync("AA-20004", to: "Jimma", seats: 10);
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(10);
        var created = await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var schedule = await created.Content.ReadFromJsonAsync<ScheduleResponse>();
        var response = await TicketerClient().GetAsync($"/api/schedules/{schedule!.Id}/seats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SeatMapResponse>();
        body!.Seats.Should().HaveCount(10);
        body.AvailableSeatCount.Should().Be(10);
        body.SoldSeatCount.Should().Be(0);
        body.IsFullySold.Should().BeFalse();
        body.Seats.First().SeatNumber.Should().Be(1);
        body.Seats.Last().SeatNumber.Should().Be(10);
    }

    [Fact]
    public async Task GetSeatStatus_WhenAvailable_ReturnsAvailable()
    {
        var scheduleId = await SeedScheduleAsync("AA-20006", 15);
        var response = await TicketerClient().GetAsync($"/api/schedules/{scheduleId}/seats/3");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SeatStatusResponse>();
        body!.SeatNumber.Should().Be(3);
        body.Status.Should().Be(SeatStatuses.Available);
    }

    [Fact]
    public async Task CreateSchedule_ForSameBusSameDay_ReturnsConflict()
    {
        var (routeId, busId) = await SeedRouteAndBusAsync("AA-20005", to: "Jimma");
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(11);
        await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var adamaCityId = await EnsureCityAsync("Adama");
        var route2 = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(adamaCityId));
        var route2Body = await route2.Content.ReadFromJsonAsync<RouteResponse>();
        var response = await client.PostAsJsonAsync("/api/schedules",
            new CreateScheduleRequest(route2Body!.Id, busId, departure.AddHours(2), 1));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

[Collection("Api")]
public sealed class TicketEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task Ticketer_HasSeededStationAssignment()
    {
        var response = await AdminClient().GetAsync(
            $"/api/users/{TestDataSeeder.TicketerId}/station-assignments");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<List<UserStationAssignmentSummaryResponse>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SellCashTicket_AsTicketer_ConfirmsTicket()
    {
        var scheduleId = await SeedScheduleAsync("AA-30001", 20);
        var client = TicketerClient();
        var response = await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 5, "Abebe Kebede", "0911223344", "NAT-123"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SellCashTicketResponse>();
        body!.Ticket.SeatNumber.Should().Be(5);
        body.Ticket.NationalId.Should().Be("NAT-123");
        body.Ticket.Price.Should().Be(346 * 2.50m);
        body.Ticket.PaymentMethod.Should().Be("Cash");
        body.ScheduleSoldSeatCount.Should().Be(1);
        body.ScheduleAvailableSeatCount.Should().Be(19);
        body.ScheduleIsFullySold.Should().BeFalse();
    }

    [Fact]
    public async Task SellCashTicket_WithoutNationalId_Succeeds()
    {
        var scheduleId = await SeedScheduleAsync("AA-30002", 20);
        var client = TicketerClient();
        var response = await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 6, "Tigist Haile", "0911223355", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SellCashTicket_ForSoldSeat_ReturnsConflict()
    {
        var scheduleId = await SeedScheduleAsync("AA-30003", 20);
        var client = TicketerClient();
        await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 1, "First", "0911000001", null));
        var response = await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 1, "Second", "0911000002", null));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTicketById_ReturnsTicket()
    {
        var scheduleId = await SeedScheduleAsync("AA-30004", 20);
        var client = TicketerClient();
        var sold = await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 2, "Get By Id", "0911000010", null));
        var ticket = await sold.Content.ReadFromJsonAsync<SellCashTicketResponse>();
        var response = await client.GetAsync($"/api/tickets/{ticket!.Ticket.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTicketById_WhenMissing_ReturnsNotFound()
    {
        var response = await TicketerClient().GetAsync($"/api/tickets/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchTickets_ByScheduleId_ReturnsMatches()
    {
        var scheduleId = await SeedScheduleAsync("AA-30005", 20);
        var client = TicketerClient();
        await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 3, "Search Me", "0911999888", null));
        var response = await client.GetAsync($"/api/tickets?scheduleId={scheduleId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SellCashTicket_ThenSeatMapShowsSold()
    {
        var scheduleId = await SeedScheduleAsync("AA-30007", 20);
        var client = TicketerClient();
        await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 7, "Sold Seat Test", "0911000077", null));

        var seatMap = await client.GetAsync($"/api/schedules/{scheduleId}/seats");
        seatMap.StatusCode.Should().Be(HttpStatusCode.OK);
        var map = await seatMap.Content.ReadFromJsonAsync<SeatMapResponse>();
        map!.SoldSeatCount.Should().Be(1);
        map.AvailableSeatCount.Should().Be(19);
        map.Seats.Single(x => x.SeatNumber == 7).Status.Should().Be(SeatStatuses.Sold);
        map.Seats.Single(x => x.SeatNumber == 8).Status.Should().Be(SeatStatuses.Available);

        var seatStatus = await client.GetAsync($"/api/schedules/{scheduleId}/seats/7");
        seatStatus.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await seatStatus.Content.ReadFromJsonAsync<SeatStatusResponse>();
        status!.Status.Should().Be(SeatStatuses.Sold);
    }

    [Fact]
    public async Task SellCashTicket_AsAdmin_ReturnsForbidden()
    {
        var scheduleId = await SeedScheduleAsync("AA-30006", 20);
        var response = await AdminClient().PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 4, "Admin Try", "0911000099", null));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SellCashTicket_WhenOriginStationMismatch_ReturnsBadRequest()
    {
        var scheduleId = await SeedScheduleAsync("AA-30007", 20);
        var adamaStationId = await GetDefaultStationIdForCityAsync("Adama");
        var admin = AdminClient();

        await admin.PostAsJsonAsync(
            $"/api/users/{TestDataSeeder.TicketerId}/station-assignments",
            new CreateUserStationAssignmentRequest(adamaStationId));

        var ticketer = TicketerClient();
        await ticketer.PutAsJsonAsync(
            "/api/auth/me/selected-station",
            new SetSelectedStationRequest(adamaStationId));

        var response = await ticketer.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 4, "Wrong Station", "0911000100", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SellCashTicket_WhenMultipleAssignmentsWithoutSelection_ReturnsBadRequest()
    {
        var scheduleId = await SeedScheduleAsync("AA-30008", 20);
        var adamaStationId = await GetDefaultStationIdForCityAsync("Adama");
        var admin = AdminClient();

        await admin.PostAsJsonAsync(
            $"/api/users/{TestDataSeeder.TicketerId}/station-assignments",
            new CreateUserStationAssignmentRequest(adamaStationId));

        var response = await TicketerClient().PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 4, "No Selection", "0911000101", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

[Collection("Api")]
public sealed class PaymentSettingsEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task GetPaymentSettings_ReturnsDisabledByDefault()
    {
        var client = AdminClient();
        var response = await client.GetAsync("/api/settings/payments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentSettingsResponse>();
        body!.OnlinePaymentEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePaymentSettings_TogglesOnlinePaymentFlag()
    {
        var client = AdminClient();
        var response = await client.PutAsJsonAsync("/api/settings/payments", new UpdatePaymentSettingsRequest(true));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentSettingsResponse>();
        body!.OnlinePaymentEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePaymentSettings_AsTicketer_ReturnsForbidden()
    {
        var response = await TicketerClient().PutAsJsonAsync("/api/settings/payments", new UpdatePaymentSettingsRequest(true));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public abstract class EndpointTestBase : IAsyncLifetime
{
    private static readonly Dictionary<string, decimal> CityDistances = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Addis Ababa"] = 0,
        ["Adama"] = 99,
        ["Hawassa"] = 275,
        ["Jimma"] = 346,
        ["Dire Dawa"] = 515,
        ["Bahir Dar"] = 565,
        ["Gondar"] = 748,
        ["Mekelle"] = 783
    };

    protected EndpointTestBase(TicketSystemWebApplicationFactory factory) => Factory = factory;

    protected TicketSystemWebApplicationFactory Factory { get; }

    protected HttpClient AdminClient() =>
        Factory.CreateClientWithCredentials("admin", TestDataSeeder.AdminWorkingPassword);

    protected HttpClient TicketerClient() =>
        Factory.CreateClientWithCredentials("ticketer", TestDataSeeder.TicketerWorkingPassword);

    protected async Task<Guid> EnsureCityAsync(string name)
    {
        var client = AdminClient();
        var all = await client.GetAsync("/api/cities");
        all.EnsureSuccessStatusCode();
        var existing = await all.Content.ReadFromJsonAsync<List<CityResponse>>();
        var match = existing!.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return match.Id;
        }

        var distance = CityDistances.TryGetValue(name, out var km) ? km : 100;
        var created = await client.PostAsJsonAsync("/api/cities", new CreateCityRequest(name, distance));
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<CityResponse>())!.Id;
    }

    protected async Task<(Guid RouteId, Guid BusId)> SeedRouteAndBusAsync(
        string plate = "AA-10001",
        string to = "Jimma",
        int seats = 45)
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync(to);
        var routeResponse = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        var route = await routeResponse.Content.ReadFromJsonAsync<RouteResponse>();
        var side = plate.Replace("AA-", "S-");
        var busResponse = await client.PostAsJsonAsync("/api/buses",
            new CreateBusRequest("Owner", "0911000000", "0911000001", side, plate, seats));
        var bus = await busResponse.Content.ReadFromJsonAsync<BusResponse>();
        return (route!.Id, bus!.Id);
    }

    protected async Task<Guid> SeedScheduleAsync(string plate, int seats, string to = "Jimma")
    {
        var (routeId, busId) = await SeedRouteAndBusAsync(plate, to, seats);
        var client = AdminClient();
        var departure = AddisTestTimes.TodayAt(12);
        var created = await client.PostAsJsonAsync("/api/schedules", new CreateScheduleRequest(routeId, busId, departure, 1));
        var schedule = await created.Content.ReadFromJsonAsync<ScheduleResponse>();
        return schedule!.Id;
    }

    protected async Task<Guid> GetDefaultStationIdForCityAsync(string cityName)
    {
        var client = AdminClient();
        await EnsureCityAsync("Addis Ababa");
        var toCityId = await EnsureCityAsync(cityName);
        var routeResponse = await client.PostAsJsonAsync("/api/routes", new CreateRouteRequest(toCityId));
        routeResponse.EnsureSuccessStatusCode();
        var route = await routeResponse.Content.ReadFromJsonAsync<RouteResponse>();
        return cityName.Equals("Addis Ababa", StringComparison.OrdinalIgnoreCase)
            ? route!.FromStation.Id
            : route!.ToStation.Id;
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<TicketSystemWebApplicationFactory>;
