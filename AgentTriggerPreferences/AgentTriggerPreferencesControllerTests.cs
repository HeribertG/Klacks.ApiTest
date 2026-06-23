// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentTriggerPreferencesController (api/backend/assistant/trigger-preferences).
 * Covers auth enforcement (401 without token), listing all known trigger kinds, and update validation.
 */

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.ApiTest.AgentTriggerPreferences;

[TestFixture]
public class AgentTriggerPreferencesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/trigger-preferences";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListPreferences_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdatePreference_WithoutToken_Returns401()
    {
        var payload = new UpdateTriggerPreferenceRequest { Muted = true };

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{AgentTriggerKinds.UnstaffedShift}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task ListPreferences_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ListPreferences_WithUserRole_ReturnsAllKnownKinds()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);
        var prefs = await response.Content.ReadFromJsonAsync<List<TriggerPreferenceDto>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        prefs.ShouldNotBeNull();
        prefs.Count.ShouldBe(6);
    }

    // ── PUT update ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePreference_WithUserRole_ValidKind_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var payload = new UpdateTriggerPreferenceRequest { Muted = true };

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{AgentTriggerKinds.UnstaffedShift}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdatePreference_WithUserRole_UnknownKind_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.User);
        var payload = new UpdateTriggerPreferenceRequest { Muted = true };

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/unknown_kind_xyz", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
