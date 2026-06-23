// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for TtsController (api/backend/assistant/tts).
 * Covers authorization enforcement (401) for all endpoints.
 */

namespace Klacks.ApiTest.Tts;

[TestFixture]
public class TtsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/tts";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Tts_GetVoices_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/voices");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Tts_PostTest_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/test", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Tts_PostSynthesize_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/synthesize", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
