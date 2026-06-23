// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AssignedGroupsController (api/backend/AssignedGroups).
 * Covers auth enforcement (401), GET list endpoint, and role-based access.
 * AssignedGroupsController only exposes a GET list endpoint; write operations
 * are inherited from InputBaseController but role-restricted.
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.AssignedGroups;

[TestFixture]
public class AssignedGroupsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/AssignedGroups";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAssignedGroups_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/list");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAssignedGroup_WithoutToken_Returns401()
    {
        var payload = new GroupResource { Name = $"{TestPrefix}Group" };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAssignedGroup_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new GroupResource { Name = $"{TestPrefix}Group" };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAssignedGroups_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/list");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<GroupResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetAssignedGroups_WithClientId_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/list?id={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<GroupResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAssignedGroup_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
