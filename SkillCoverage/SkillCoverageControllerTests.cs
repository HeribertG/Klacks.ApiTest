// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SkillCoverageController (api/backend/assistant/coverage).
 * Any authenticated user may access coverage; covers 401 without token and a
 * tolerant authenticated check (not Unauthorized) because ComputeAsync reads a
 * doc file that may or may not be present under the test host's content root.
 */

namespace Klacks.ApiTest.SkillCoverage;

[TestFixture]
public class SkillCoverageControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/coverage";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCoverage_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated (tolerant: accepts 200 or non-auth error) ─────────────

    [Test]
    public async Task GetCoverage_WithUserRole_IsAuthenticated()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetCoverage_WithAdminRole_IsAuthenticated()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
    }
}
