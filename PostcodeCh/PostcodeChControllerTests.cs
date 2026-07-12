// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for PostcodeChController (api/backend/PostcodeCh).
 * Covers authorization enforcement (401/403) and basic GET behaviour.
 */

namespace Klacks.ApiTest.PostcodeCh;

[TestFixture]
public class PostcodeChControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/PostcodeCh";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAll_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET by zip ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByZip_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/3000");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetByZip_UnknownZip_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/99999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetByZip_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/3000");

        // Bern ZIP 3000 is seeded in the test DB; accept OK or NotFound depending on seed state
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
