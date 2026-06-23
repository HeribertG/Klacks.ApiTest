// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentAutonomyController (api/backend/assistant/autonomy-level).
 * Covers auth enforcement (401 without token) and GET/PUT behaviour for authenticated users.
 */

namespace Klacks.ApiTest.AgentAutonomy;

[TestFixture]
public class AgentAutonomyControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/autonomy-level";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyLevel_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SetMyLevel_WithoutToken_Returns401()
    {
        var payload = new { Level = 1 };

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyLevel_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMyLevel_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task SetMyLevel_ValidLevel_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Level = 1 };

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task SetMyLevel_InvalidLevel_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Level = 999 };

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
