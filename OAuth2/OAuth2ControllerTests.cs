// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for OAuth2Controller (api/backend/OAuth2).
 * All endpoints are AllowAnonymous — covers reachability and basic input validation.
 */

namespace Klacks.ApiTest.OAuth2;

[TestFixture]
public class OAuth2ControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/OAuth2";

    // ── providers (anonymous) ────────────────────────────────────────────────

    [Test]
    public async Task GetProviders_WithoutToken_Returns200()
    {
        var response = await Client.GetAsync($"{BaseRoute}/providers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── logout-url (anonymous) ───────────────────────────────────────────────

    [Test]
    public async Task GetLogoutUrl_UnknownProvider_Returns200OrNotFound()
    {
        var unknownId = Guid.NewGuid();

        var response = await Client.GetAsync($"{BaseRoute}/logout-url/{unknownId}");

        // No auth required; may return 200 with null or 404 depending on implementation
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── authorize (anonymous) ────────────────────────────────────────────────

    [Test]
    public async Task Authorize_UnknownProvider_Returns200OrNotFound()
    {
        var unknownId = Guid.NewGuid();

        var response = await Client.GetAsync($"{BaseRoute}/authorize/{unknownId}?redirectUri=https://example.com");

        // No auth required; endpoint is reachable
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // ── callback (anonymous) ─────────────────────────────────────────────────

    [Test]
    public async Task Callback_WithNullBody_Returns415()
    {
        var response = await Client.PostAsync($"{BaseRoute}/callback", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }
}
