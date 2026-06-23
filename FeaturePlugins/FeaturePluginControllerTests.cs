// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for FeaturePluginController (api/plugins/features).
 * Covers public GET endpoints, auth enforcement (401/403) for admin-only actions.
 */

namespace Klacks.ApiTest.FeaturePlugins;

[TestFixture]
public class FeaturePluginControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/plugins/features";

    // ── Public GET list ──────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPlugins_WithoutToken_ReturnsOk()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetPlugin_WithoutToken_UnknownName_Returns404()
    {
        var response = await Client.GetAsync($"{BaseRoute}/nonexistent-plugin");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Admin-only: install ──────────────────────────────────────────────────

    [Test]
    public async Task InstallPlugin_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/install", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InstallPlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/install", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task InstallPlugin_WithAdminRole_UnknownPlugin_ReturnsBadRequest()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/nonexistent-plugin/install", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Admin-only: uninstall ─────────────────────────────────────────────────

    [Test]
    public async Task UninstallPlugin_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/myplugin/uninstall");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UninstallPlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/myplugin/uninstall");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Admin-only: enable/disable ────────────────────────────────────────────

    [Test]
    public async Task EnablePlugin_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/enable", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EnablePlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/enable", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DisablePlugin_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/disable", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DisablePlugin_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/myplugin/disable", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
