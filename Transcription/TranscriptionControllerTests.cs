// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for TranscriptionController (api/backend/assistant/transcription).
 * Covers authorization enforcement (401) for all endpoints.
 */

namespace Klacks.ApiTest.Transcription;

[TestFixture]
public class TranscriptionControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/transcription";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Transcription_PostEnhance_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/enhance", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
