// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for LoadFileController (api/backend/LoadFile).
 * Covers auth enforcement (401) for DELETE, POST upload, and GET download.
 */

namespace Klacks.ApiTest.LoadFile;

[TestFixture]
public class LoadFileControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/LoadFile";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteLoadFile_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/somefiletype");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostUpload_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/Upload", new MultipartFormDataContent());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetDownload_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/DownLoad?type=own-logo.png");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
