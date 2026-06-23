// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AnalyseScenariosController (api/backend/AnalyseScenarios).
 * Authenticated endpoint — any valid role is accepted (no admin restriction).
 * Tests cover 401 enforcement, 404 for unknown resources, and 400 for missing required params.
 */

namespace Klacks.ApiTest.AnalyseScenarios;

[TestFixture]
public class AnalyseScenariosControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/AnalyseScenarios";

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetList_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ───────────────────────────────────────────────────────────

    [Test]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST create ──────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { name = "TestScenario" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── DELETE all — requires groupId ────────────────────────────────────────

    [Test]
    public async Task DeleteAll_WithoutGroupId_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Accept / Reject unknown IDs ──────────────────────────────────────────

    [Test]
    public async Task Accept_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Reject_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}/Reject", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
