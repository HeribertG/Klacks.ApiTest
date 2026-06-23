// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SkillProposalsController (api/backend/assistant/skill-proposals).
 * Entire controller is Admin-only; covers 401/403 enforcement, GET pending list, and
 * 400 (not 404) for approve/reject on an unknown proposal ID.
 */

namespace Klacks.ApiTest.SkillProposals;

[TestFixture]
public class SkillProposalsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/skill-proposals";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPending_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/pending");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetPending_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/pending");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetPending_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/pending");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Generate_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/generate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Generate_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/generate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Approve_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Approve_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Reject_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/reject", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Reject_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/reject", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET pending list ─────────────────────────────────────────────────────

    [Test]
    public async Task GetPending_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/pending");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Approve / Reject unknown ID → 400 (not 404) ──────────────────────────

    [Test]
    public async Task Approve_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Reject_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/reject", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
