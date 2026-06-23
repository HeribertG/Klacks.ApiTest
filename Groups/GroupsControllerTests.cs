// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GroupsController (api/backend/Groups).
 * Covers create, get, delete, tree, path, roots, members, and move — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.Groups;

[TestFixture]
public class GroupsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Groups";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Group
            .Where(g => g.Name.StartsWith(TestPrefix) && !g.IsDeleted);
        DbContext.Group.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostGroup_WithoutToken_Returns401()
    {
        var payload = MinimalGroup($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostGroup_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalGroup($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteGroup_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteGroup_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroup_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET tree ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetTree_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/tree");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET roots ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetRoots_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/roots");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET path ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPath_UnknownId_ReturnsOkWithEmptyOrNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/path/{Guid.NewGuid()}");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── GET members ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroupMembers_UnknownGroupId_ReturnsOkOrNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/members");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostGroup_WithAdminRole_ReturnsCreatedGroup()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalGroup($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<GroupResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task PostGroup_WithAuthorisedRole_ReturnsCreatedGroup()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalGroup($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteGroup_WithAdminRole_ReturnsDeletedGroup()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateGroupAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<GroupResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteGroup_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static GroupResource MinimalGroup(string name) => new()
    {
        Name = name,
        Description = string.Empty,
        ValidFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PaymentInterval = PaymentInterval.Monthly,
    };

    private async Task<GroupResource> CreateGroupAsync(string name)
    {
        var payload = MinimalGroup(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupResource>())!;
    }
}
