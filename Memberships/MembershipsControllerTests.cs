// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for MembershipsController (api/backend/Memberships).
 * Covers list and auth enforcement (401).
 * MembershipsController exposes only GET (list); POST/DELETE are inherited but not covered here
 * because creating a membership requires a valid ClientId from the staffs domain.
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.Memberships;

[TestFixture]
public class MembershipsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Memberships";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMemberships_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostMembership_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new MembershipResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostMembership_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(BaseRoute, new MembershipResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteMembership_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteMembership_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetMembership_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMemberships_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var memberships = await response.Content.ReadFromJsonAsync<IEnumerable<MembershipResource>>();
        memberships.ShouldNotBeNull();
    }

    [Test]
    public async Task DeleteMembership_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
