// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for VersionController (GET /api/Version).
 * No authentication required — validates that the endpoint is reachable
 * and returns a well-formed version payload.
 */

namespace Klacks.ApiTest.Version;

[TestFixture]
public class VersionControllerTests : ApiTestBase
{
    [Test]
    public async Task GetVersion_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/Version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetVersion_ReturnsVersionPayload()
    {
        var response = await Client.GetAsync("/api/Version");
        var body = await response.Content.ReadFromJsonAsync<VersionResponse>();

        body.ShouldNotBeNull();
        body!.VersionString.ShouldNotBeNullOrWhiteSpace();
    }

    private sealed record VersionResponse(
        string Variant,
        int Major,
        int Minor,
        int Patch,
        string BuildKey,
        string BuildTimestamp,
        string VersionString,
        string VersionStringWithBuildInfo);
}
