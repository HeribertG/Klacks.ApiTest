// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SttController (api/backend/assistant/stt).
 * Covers authorization enforcement (401) for all testable endpoints.
 * Note: WebSocket stream endpoint is not testable via plain HTTP client.
 */

namespace Klacks.ApiTest.Stt;

[TestFixture]
public class SttControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/stt";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Stt_GetProviders_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/providers");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Stt_PostTest_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/test", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Stt_PostTranscribe_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/transcribe", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
