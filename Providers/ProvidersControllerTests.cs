// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ProvidersController (api/backend/assistant/providers).
 * GET endpoints are available to any authenticated user; discover/POST/PUT/DELETE require Admin.
 */

namespace Klacks.ApiTest.Providers;

[TestFixture]
public class ProvidersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/providers";

    // ── Auth — GET list ─────────────────────────────────────────────────────

    [Test]
    public async Task GetProviders_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProviders_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetProviders_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetProvider_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProvider_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Auth — Admin-only mutations ─────────────────────────────────────────

    [Test]
    public async Task DiscoverProviders_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/discover", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DiscoverProviders_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/discover", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateProvider_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateProvider_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateProvider_WithoutToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateProvider_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteProvider_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteProvider_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteProvider_UnknownId_WithAdminRole_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
