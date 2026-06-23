// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ModelSyncController (api/backend/assistant/sync-notifications).
 * Covers auth enforcement (401 without token) for all endpoints.
 */

namespace Klacks.ApiTest.ModelSync;

[TestFixture]
public class ModelSyncControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/sync-notifications";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetNotifications_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TriggerSync_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/trigger", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task MarkRead_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/mark-read", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetHistory_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/history");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
