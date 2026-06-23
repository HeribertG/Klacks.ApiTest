// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for LanguageConfigController (api/config).
 * Covers public endpoints (no auth required) and auth-gated admin endpoints (401/403).
 */

namespace Klacks.ApiTest.LanguageConfig;

[TestFixture]
public class LanguageConfigControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/config";

    // ── Public endpoints ─────────────────────────────────────────────────────

    [Test]
    public async Task GetLanguages_WithoutToken_ReturnsOk()
    {
        var response = await Client.GetAsync($"{BaseRoute}/languages");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetTranslations_WithoutToken_ReturnsOk()
    {
        var response = await Client.GetAsync($"{BaseRoute}/translations/de");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetLanguagePlugins_WithoutToken_ReturnsOk()
    {
        var response = await Client.GetAsync($"{BaseRoute}/language-plugins");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Auth-gated: plugin doc ───────────────────────────────────────────────

    [Test]
    public async Task GetPluginDoc_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/language-plugins/de/docs/guide");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetPluginDoc_WithUserRole_UnknownPlugin_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/language-plugins/nonexistent/docs/guide");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Auth-gated: admin install/uninstall ──────────────────────────────────

    [Test]
    public async Task InstallPlugin_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/language-plugins/testplugin/install", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InstallPlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/language-plugins/testplugin/install", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UninstallPlugin_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/language-plugins/testplugin/uninstall");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UninstallPlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/language-plugins/testplugin/uninstall");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Auth-gated: marketplace ──────────────────────────────────────────────

    [Test]
    public async Task SearchMarketplacePackages_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/marketplace/packages");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SearchMarketplacePackages_WithUserRole_ReturnsOkOrServiceUnavailable()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/marketplace/packages");

        // Marketplace may not be configured in the test environment
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}
