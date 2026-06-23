// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for DashboardController (api/backend/Dashboard).
 * Covers client locations, shift coverage statistics, resource monitor, and auth enforcement (401).
 */

namespace Klacks.ApiTest.Dashboard;

[TestFixture]
public class DashboardControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Dashboard";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetClientLocations_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ClientLocations");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetShiftCoverageStatistics_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ShiftCoverageStatistics");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetResourceMonitor_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ResourceMonitor?year=2026");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET ClientLocations ──────────────────────────────────────────────────

    [Test]
    public async Task GetClientLocations_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ClientLocations");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetClientLocations_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/ClientLocations");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET ShiftCoverageStatistics ──────────────────────────────────────────

    [Test]
    public async Task GetShiftCoverageStatistics_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ShiftCoverageStatistics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetShiftCoverageStatistics_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/ShiftCoverageStatistics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET ResourceMonitor ──────────────────────────────────────────────────

    [Test]
    public async Task GetResourceMonitor_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ResourceMonitor?year=2026");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetResourceMonitor_WithGroupId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ResourceMonitor?year=2026&groupId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetResourceMonitor_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/ResourceMonitor?year=2026");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
