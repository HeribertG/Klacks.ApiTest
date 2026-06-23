// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for TranslationController (api/backend/Translation).
 * Covers authorization enforcement (401 without token), status endpoint, and
 * input validation for the translate-all endpoint.
 */

using System.Text;
using System.Text.Json;

namespace Klacks.ApiTest.Translation;

[TestFixture]
public class TranslationControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Translation";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetStatus_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/status");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TranslateAll_WithoutToken_Returns401()
    {
        var payload = new { Text = "Hello", SourceLanguage = "en" };
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/translate-all", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET status ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetStatus_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST translate-all validation ────────────────────────────────────────

    [Test]
    public async Task TranslateAll_EmptyText_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Text = "", SourceLanguage = "en" };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/translate-all", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task TranslateAll_UnsupportedLanguage_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Text = "Hello", SourceLanguage = "xx" };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/translate-all", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
