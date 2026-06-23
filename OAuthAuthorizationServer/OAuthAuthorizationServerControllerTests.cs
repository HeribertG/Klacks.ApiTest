// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for OAuthAuthorizationServerController (oauth/*).
 * All endpoints are AllowAnonymous. Tests cover registration, authorize, and token endpoints
 * with invalid or missing parameters to verify error handling without auth.
 */

namespace Klacks.ApiTest.OAuthAuthorizationServer;

[TestFixture]
public class OAuthAuthorizationServerControllerTests : ApiTestBase
{
    private const string BaseRoute = "/oauth";

    // ── register ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Register_WithNullBody_Returns415()
    {
        var response = await Client.PostAsync($"{BaseRoute}/register", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Register_WithEmptyRequest_ReturnsBadRequestOrCreated()
    {
        var payload = new { redirect_uris = Array.Empty<string>() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", payload);

        // Invalid registration metadata returns 400
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── authorize GET ────────────────────────────────────────────────────────

    [Test]
    public async Task Authorize_Get_WithNoQueryParams_ReturnsBadRequest()
    {
        var response = await Client.GetAsync($"{BaseRoute}/authorize");

        // No valid client_id or redirect_uri — cannot redirect, returns 400
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Authorize_Get_WithUnknownClientId_ReturnsBadRequest()
    {
        var clientId = Guid.NewGuid().ToString();
        var url = $"{BaseRoute}/authorize?client_id={clientId}&redirect_uri=https://example.com&response_type=code&code_challenge=abc&code_challenge_method=S256";

        var response = await Client.GetAsync(url);

        // Unknown client cannot be validated, redirect forbidden for unknown redirect_uri
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── token ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Token_WithNoFormBody_Returns400()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>());

        var response = await Client.PostAsync($"{BaseRoute}/token", content);

        // Missing grant_type → invalid_request
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Token_WithUnsupportedGrantType_Returns400()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        var response = await Client.PostAsync($"{BaseRoute}/token", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Token_WithInvalidAuthorizationCode_ReturnsError()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = "invalid-code",
            ["redirect_uri"] = "https://example.com",
            ["client_id"] = Guid.NewGuid().ToString(),
            ["code_verifier"] = "invalid-verifier"
        });

        var response = await Client.PostAsync($"{BaseRoute}/token", content);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }
}
