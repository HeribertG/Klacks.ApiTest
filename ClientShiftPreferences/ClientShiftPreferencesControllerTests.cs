// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ClientShiftPreferencesController (api/backend/ClientShiftPreferences).
 * Covers auth enforcement (401 without token) and happy-path GETs for any authenticated user.
 */

namespace Klacks.ApiTest.ClientShiftPreferences;

[TestFixture]
public class ClientShiftPreferencesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ClientShiftPreferences";

    [TearDown]
    public new void BaseTearDown() { base.BaseTearDown(); }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetByClient_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}?clientId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAvailableShifts_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/available-shifts?clientId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SaveAll_WithoutToken_Returns401()
    {
        var payload = new { ClientId = Guid.NewGuid(), Preferences = new object[] { } };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/bulk", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET preferences ──────────────────────────────────────────────────────

    [Test]
    public async Task GetByClient_WithUserRole_WithAnyClientId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}?clientId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET available shifts ──────────────────────────────────────────────────

    [Test]
    public async Task GetAvailableShifts_WithUserRole_WithAnyClientId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/available-shifts?clientId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
