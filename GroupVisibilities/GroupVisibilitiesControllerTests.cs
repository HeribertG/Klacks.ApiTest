// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GroupVisibilitiesController (api/backend/GroupVisibilities).
 * Covers list, get — including auth enforcement (401/403/404).
 * GroupVisibilitiesController is Admin only at class level, reads included.
 * Full creation roundtrip is skipped as it requires an existing AppUser and Group.
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.GroupVisibilities;

[TestFixture]
public class GroupVisibilitiesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/GroupVisibilities";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroupVisibilityList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteGroupVisibility_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteGroupVisibility_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list (Admin only since 2026-08-14) ──────────────────────────────

    [Test]
    public async Task GetSimpleList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetSimpleList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<GroupVisibilityResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetPersonalSimpleList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var userId = Guid.NewGuid().ToString();

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList/{userId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetPersonalSimpleList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);
        var userId = Guid.NewGuid().ToString();

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList/{userId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroupVisibility_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteGroupVisibility_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
