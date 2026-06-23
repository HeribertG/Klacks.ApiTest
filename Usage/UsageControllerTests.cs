// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for UsageController (api/backend/assistant/usage).
 * Any authenticated user may access usage stats; covers 401 enforcement,
 * happy-path GET, and deterministic validation errors on the /export stub.
 */

namespace Klacks.ApiTest.Usage;

[TestFixture]
public class UsageControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/usage";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetUsageStatistics_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetExport_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/export");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET usage ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetUsageStatistics_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetUsageStatistics_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}?days=7");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET export — deterministic validation (no DB required) ──────────────

    [Test]
    public async Task GetExport_InvalidFormat_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/export?format=xml&days=30");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetExport_NegativeDays_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/export?format=csv&days=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetExport_DaysExceedsLimit_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/export?format=csv&days=366");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetExport_ValidParams_Returns501()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/export?format=csv&days=30");

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }
}
