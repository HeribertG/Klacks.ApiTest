// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for OAuthAuthorizationServerMetadataController (.well-known/oauth-authorization-server).
 * This endpoint is public (AllowAnonymous) and serves RFC 8414 metadata.
 */

namespace Klacks.ApiTest.OAuthAuthorizationServerMetadata;

[TestFixture]
public class OAuthAuthorizationServerMetadataControllerTests : ApiTestBase
{
    private const string MetadataRoute = "/.well-known/oauth-authorization-server";

    // ── Metadata endpoint (anonymous, public) ────────────────────────────────

    [Test]
    public async Task GetAuthorizationServerMetadata_WithoutToken_Returns200()
    {
        var response = await Client.GetAsync(MetadataRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAuthorizationServerMetadata_ReturnsJsonContent()
    {
        var response = await Client.GetAsync(MetadataRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Test]
    public async Task GetAuthorizationServerMetadata_WithAdminToken_Returns200()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(MetadataRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
