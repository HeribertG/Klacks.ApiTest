// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentPlansController (api/backend/assistant/plans).
 * Covers auth enforcement (401 without token) and basic CRUD/approval response shapes.
 */

namespace Klacks.ApiTest.AgentPlans;

[TestFixture]
public class AgentPlansControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/plans";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListMyPlans_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetPlan_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreatePlan_WithoutToken_Returns401()
    {
        var payload = new { Goal = "Create a shift for next Monday", SessionId = (string?)null };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApprovePlan_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListMyPlans_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetPlan_WithUserRole_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST create ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePlan_EmptyGoal_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Goal = "", SessionId = (string?)null };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST approve unknown plan ─────────────────────────────────────────────

    [Test]
    public async Task ApprovePlan_WithUserRole_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
