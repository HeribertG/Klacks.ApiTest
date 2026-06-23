// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for EvalController (api/backend/assistant/eval).
 * Covers auth enforcement (401 without token) and role-based access (403 for non-Admin on restricted endpoints).
 */

namespace Klacks.ApiTest.Eval;

[TestFixture]
public class EvalControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/eval";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task RunEval_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/run?goldset=test", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RunEval_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/run?goldset=test", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetRuns_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/runs?goldset=test");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetRuns_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/runs?goldset=test");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SubmitCorrection_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/correction", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
