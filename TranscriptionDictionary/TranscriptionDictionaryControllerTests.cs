// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for TranscriptionDictionaryController (api/backend/assistant/transcription/dictionary).
 * Covers authorization enforcement (401) for all endpoints.
 */

namespace Klacks.ApiTest.TranscriptionDictionary;

[TestFixture]
public class TranscriptionDictionaryControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/transcription/dictionary";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task TranscriptionDictionary_Get_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TranscriptionDictionary_Post_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TranscriptionDictionary_Put_WithoutToken_Returns401()
    {
        var id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{id}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TranscriptionDictionary_Delete_WithoutToken_Returns401()
    {
        var id = Guid.NewGuid();

        var response = await Client.DeleteAsync($"{BaseRoute}/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
