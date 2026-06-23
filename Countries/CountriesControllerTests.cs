// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for CountriesController (api/backend/Countries).
 * Covers authorization enforcement (401 without token) and successful list retrieval.
 */

namespace Klacks.ApiTest.Countries;

[TestFixture]
public class CountriesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Countries";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSingle_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
