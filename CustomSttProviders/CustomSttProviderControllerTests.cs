// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for CustomSttProviderController (api/backend/assistant/stt/providers/custom).
 * Covers auth enforcement (401 without token) for all CRUD endpoints.
 */

namespace Klacks.ApiTest.CustomSttProviders;

[TestFixture]
public class CustomSttProviderControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/stt/providers/custom";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Update_WithoutToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
