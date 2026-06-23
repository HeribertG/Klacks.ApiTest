// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for DocsController (api/backend/assistant/docs).
 * Covers auth enforcement (401 without token) and happy-path GET for authenticated users.
 */

namespace Klacks.ApiTest.Docs;

[TestFixture]
public class DocsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/docs";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetDoc_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/someDoc");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetDocRaw_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/someDoc/raw");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list (any authenticated role) ──────────────────────────────────

    [Test]
    public async Task GetAll_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
