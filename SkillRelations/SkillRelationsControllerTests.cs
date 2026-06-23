// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SkillRelationsController (api/backend/assistant/skill-relations).
 * Entire controller is Admin-only; covers 401/403 enforcement and GET happy-path.
 */

namespace Klacks.ApiTest.SkillRelations;

[TestFixture]
public class SkillRelationsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/skill-relations";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAll_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetAll_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Accept_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Accept_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Dismiss_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/dismiss", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Dismiss_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/dismiss", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST accept / dismiss unknown ID → 404 ──────────────────────────────

    [Test]
    public async Task Accept_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Dismiss_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/dismiss", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
