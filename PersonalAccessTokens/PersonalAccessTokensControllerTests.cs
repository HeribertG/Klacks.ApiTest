// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for PersonalAccessTokensController (api/backend/personal-access-tokens).
 * Covers auth enforcement (401 without token) and basic response shapes for authenticated users.
 */

namespace Klacks.ApiTest.PersonalAccessTokens;

[TestFixture]
public class PersonalAccessTokensControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/personal-access-tokens";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOwnTokens_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        var payload = new { Name = "MyToken", ExpiresInDays = 30 };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Revoke_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET own tokens ───────────────────────────────────────────────────────

    [Test]
    public async Task GetOwnTokens_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetOwnTokens_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST create ──────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithUserRole_IsNotUnauthorized()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Name = "TestToken", ExpiresInDays = 30 };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    // ── DELETE revoke ────────────────────────────────────────────────────────

    [Test]
    public async Task Revoke_WithUserRole_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
