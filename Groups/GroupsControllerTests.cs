// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for GroupsController (api/backend/Groups).
 * Covers create, get, delete, tree, refresh (POST only), path, roots, members, and move — including auth enforcement (401/403/404)
 * and the supervisor's subgroup writes (owner decision 2026-11-08).
 */

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Models.Associations;

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

    // ── POST refresh ────────────────────────────────────────────────────────

    [Test]
    public async Task RefreshTree_WithPost_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/refresh", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RefreshTree_WithGet_IsNoLongerRouted()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/refresh");

        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RefreshTree_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/refresh", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
    public async Task PostGroup_RootGroupWithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalGroup($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostGroup_SubgroupUnderVisibleGroupWithAuthorisedRole_TakesTheParentSettings()
    {
        var root = await CreateRootAsync($"{TestPrefix}SubRoot", PaymentInterval.Biweekly);
        await AuthorizeSupervisorSeeingAsync(root.Id);
        var payload = MinimalGroup($"{TestPrefix}SubEvent");
        payload.Parent = root.Id;
        payload.PaymentInterval = PaymentInterval.Weekly;

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<GroupResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = await DbContext.Group.AsNoTracking().SingleAsync(g => g.Id == created!.Id);
        stored.Parent.ShouldBe(root.Id);
        stored.PaymentInterval.ShouldBe(PaymentInterval.Biweekly);
    }

    [Test]
    public async Task PostGroup_SubgroupUnderHiddenGroupWithAuthorisedRole_Returns403()
    {
        var visibleRoot = await CreateRootAsync($"{TestPrefix}SeenRoot", PaymentInterval.Monthly);
        var hiddenRoot = await CreateRootAsync($"{TestPrefix}HiddenRoot", PaymentInterval.Monthly);
        await AuthorizeSupervisorSeeingAsync(visibleRoot.Id);
        var payload = MinimalGroup($"{TestPrefix}SubHidden");
        payload.Parent = hiddenRoot.Id;

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostGroup_SubgroupWithAShiftAssignmentWithAuthorisedRole_Returns403()
    {
        var root = await CreateRootAsync($"{TestPrefix}ShiftRoot", PaymentInterval.Monthly);
        await AuthorizeSupervisorSeeingAsync(root.Id);
        var payload = MinimalGroup($"{TestPrefix}ShiftSub");
        payload.Parent = root.Id;
        payload.GroupItems.Add(new GroupItemResource { ShiftId = Guid.NewGuid() });

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostGroup_SubgroupWithUserRole_Returns403()
    {
        var root = await CreateRootAsync($"{TestPrefix}PlannerRoot", PaymentInterval.Monthly);
        AuthorizeAs(Roles.User);
        var payload = MinimalGroup($"{TestPrefix}PlannerSub");
        payload.Parent = root.Id;

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutGroup_RootGroupWithAuthorisedRole_Returns403()
    {
        var root = await CreateRootAsync($"{TestPrefix}PutAuth", PaymentInterval.Monthly);
        await AuthorizeSupervisorSeeingAsync(root.Id);

        var response = await Client.PutAsJsonAsync(BaseRoute, root);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutGroup_RenamingASubgroupWithAuthorisedRole_ReturnsOk()
    {
        var root = await CreateRootAsync($"{TestPrefix}RenameRoot", PaymentInterval.Biweekly);
        var subPayload = MinimalGroup($"{TestPrefix}RenameSub");
        subPayload.Parent = root.Id;
        subPayload.PaymentInterval = PaymentInterval.Biweekly;
        (await Client.PostAsJsonAsync(BaseRoute, subPayload)).EnsureSuccessStatusCode();
        var subgroupId = (await DbContext.Group.AsNoTracking().SingleAsync(g => g.Name == subPayload.Name)).Id;
        await AuthorizeSupervisorSeeingAsync(root.Id);
        var subgroup = (await Client.GetFromJsonAsync<GroupResource>($"{BaseRoute}/{subgroupId}"))!;
        subgroup.Name = $"{TestPrefix}Renamed";

        var response = await Client.PutAsJsonAsync(BaseRoute, subgroup);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = await DbContext.Group.AsNoTracking().SingleAsync(g => g.Id == subgroup.Id);
        stored.Name.ShouldBe($"{TestPrefix}Renamed");
        stored.PaymentInterval.ShouldBe(PaymentInterval.Biweekly);
    }

    [Test]
    public async Task PutGroup_ChangingPaymentIntervalOfASubgroupWithAuthorisedRole_Returns403()
    {
        var (root, subgroup) = await CreateRootWithSubgroupAsync("Interval");
        await AuthorizeSupervisorSeeingAsync(root.Id);
        subgroup.PaymentInterval = PaymentInterval.Weekly;

        var response = await Client.PutAsJsonAsync(BaseRoute, subgroup);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutGroup_ChangingTheParentOfASubgroupWithAuthorisedRole_Returns403()
    {
        var (root, subgroup) = await CreateRootWithSubgroupAsync("Move");
        await AuthorizeSupervisorSeeingAsync(root.Id);
        subgroup.Parent = null;

        var response = await Client.PutAsJsonAsync(BaseRoute, subgroup);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task MoveGroup_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsync($"{BaseRoute}/move/{Guid.NewGuid()}?newParentId={Guid.NewGuid()}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SetGroupLocation_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/location", new GroupLocationResource { Latitude = 47.0, Longitude = 8.0 });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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

    private async Task<GroupResource> CreateRootAsync(string name, PaymentInterval paymentInterval)
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalGroup(name);
        payload.PaymentInterval = paymentInterval;
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupResource>())!;
    }

    private async Task<(GroupResource Root, GroupResource Subgroup)> CreateRootWithSubgroupAsync(string suffix)
    {
        var root = await CreateRootAsync($"{TestPrefix}{suffix}Root", PaymentInterval.Monthly);
        var payload = MinimalGroup($"{TestPrefix}{suffix}Sub");
        payload.Parent = root.Id;
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        var subgroup = (await response.Content.ReadFromJsonAsync<GroupResource>())!;
        return (root, subgroup);
    }

    private async Task AuthorizeSupervisorSeeingAsync(Guid rootId)
    {
        var supervisorId = AuthorizeAs(Roles.Authorised);
        DbContext.GroupVisibility.Add(new GroupVisibility { AppUserId = supervisorId, GroupId = rootId });
        await DbContext.SaveChangesAsync();
    }
}
