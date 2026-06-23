// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SkillsController (api/backend/skills).
 * Covers auth enforcement (401 without token) and happy-path GETs for any authenticated user.
 */

namespace Klacks.ApiTest.Skills;

[TestFixture]
public class SkillsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/skills";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllSkills_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSkillByName_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/some-skill-name");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAnalytics_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/analytics");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ExecuteSkill_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/execute", new { SkillName = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list (any authenticated role) ──────────────────────────────────

    [Test]
    public async Task GetAllSkills_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAllSkills_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSkillByName_UnknownName_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/nonexistent-skill-{Guid.NewGuid():N}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET analytics ───────────────────────────────────────────────────────

    [Test]
    public async Task GetAnalytics_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/analytics?days=7");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
