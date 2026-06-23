// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ClientAvailabilitiesController (api/backend/ClientAvailabilities).
 * Covers auth enforcement (401), GET list with date range, POST Bulk update,
 * and POST Clients filter. No role restriction beyond JWT auth.
 */

using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.ClientAvailabilities;

[TestFixture]
public class ClientAvailabilitiesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ClientAvailabilities";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetClientAvailabilities_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(
            $"{BaseRoute}?startDate=2026-01-01&endDate=2026-01-31");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task BulkUpdate_WithoutToken_Returns401()
    {
        var payload = new ClientAvailabilityBulkRequest { Items = [] };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Bulk", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetClients_WithoutToken_Returns401()
    {
        var payload = new ClientAvailabilityClientFilter
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Clients", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetClientAvailabilities_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(
            $"{BaseRoute}?startDate=2026-06-01&endDate=2026-06-30");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ClientAvailabilityResource>>();
        list.ShouldNotBeNull();
    }

    // ── POST Bulk ────────────────────────────────────────────────────────────

    [Test]
    public async Task BulkUpdate_EmptyItems_WithUserRole_ReturnsOkWithZero()
    {
        AuthorizeAs(Roles.User);
        var payload = new ClientAvailabilityBulkRequest { Items = [] };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Bulk", payload);
        var count = await response.Content.ReadFromJsonAsync<int>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        count.ShouldBe(0);
    }

    // ── POST Clients ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetClients_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var payload = new ClientAvailabilityClientFilter
        {
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ShowEmployees = true,
            ShowExtern = true,
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Clients", payload);
        var result = await response.Content.ReadFromJsonAsync<ClientAvailabilityClientListResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.Clients.ShouldNotBeNull();
    }
}
