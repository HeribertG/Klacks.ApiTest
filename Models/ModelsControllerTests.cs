// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ModelsController (api/backend/assistant/models).
 * GET endpoints are available to any authenticated user; mutation endpoints
 * (enable/disable/set-default/optimize/POST/PUT/DELETE) require Admin role.
 */

namespace Klacks.ApiTest.Models;

[TestFixture]
public class ModelsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/models";

    // ── Auth — GET ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAvailableModels_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAvailableModels_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAvailableModels_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Auth — Admin-only mutations ─────────────────────────────────────────

    [Test]
    public async Task OptimizeForKlacksy_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/optimize-for-klacksy", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task OptimizeForKlacksy_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/optimize-for-klacksy", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task EnableModel_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/enable", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EnableModel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/enable", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DisableModel_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/disable", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DisableModel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/disable", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SetDefaultModel_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/set-default", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SetDefaultModel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/some-model-id/set-default", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateModel_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateModel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteModel_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteModel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteModel_UnknownGuidId_WithAdminRole_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
