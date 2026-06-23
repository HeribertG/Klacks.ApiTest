// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GroupVisibilitiesController (api/backend/GroupVisibilities).
 * Covers list, get — including auth enforcement (401/403/404).
 * GroupVisibilitiesController inherits InputBaseController<GroupResource>, restricting
 * inherited mutations (POST/DELETE) to Admin/Authorised.
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

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSimpleList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<GroupVisibilityResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetPersonalSimpleList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var userId = Guid.NewGuid().ToString();

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleList/{userId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroupVisibility_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

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
