// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GlobalRulesController (api/backend/assistant/global-rules).
 * All endpoints are available to any authenticated user (no Admin restriction).
 * Covers 401 enforcement, GET happy-paths, and history endpoint.
 */

namespace Klacks.ApiTest.GlobalRules;

[TestFixture]
public class GlobalRulesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/global-rules";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetHistory_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/history");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Upsert_WithoutToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/some-rule", new { Content = "test", SortOrder = 0 });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Deactivate_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/some-rule");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET all (any authenticated role) ────────────────────────────────────

    [Test]
    public async Task GetAll_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAll_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET history ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetHistory_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/history?limit=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Upsert + Deactivate (any authenticated role) ─────────────────────────

    [Test]
    public async Task Upsert_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var ruleName = $"test-rule-{Guid.NewGuid():N}";

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{ruleName}", new { Content = "Test content", SortOrder = 99 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Deactivate_WithUserRole_ReturnsNoContent()
    {
        AuthorizeAs(Roles.User);
        var ruleName = $"test-rule-{Guid.NewGuid():N}";

        var response = await Client.DeleteAsync($"{BaseRoute}/{ruleName}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
