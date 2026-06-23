// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ScheduleChangesController (api/backend/ScheduleChanges).
 * Authenticated endpoint — any valid role is accepted (no admin restriction).
 * Tests cover 401 enforcement and reachability with required date parameters.
 */

namespace Klacks.ApiTest.ScheduleChanges;

[TestFixture]
public class ScheduleChangesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ScheduleChanges";

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetChanges_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}?startDate=2026-01-01&endDate=2026-01-31");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetChanges_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}?startDate=2026-01-01&endDate=2026-01-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetChanges_WithAdminRole_Returns200()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}?startDate=2026-01-01&endDate=2026-01-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
