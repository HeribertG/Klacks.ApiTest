// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GroupItemsController (api/backend/GroupItems).
 * Covers auth enforcement (401/403), 404 for unknown IDs, and the custom remove-by-query endpoint.
 * GroupItemsController inherits InputBaseController which restricts mutations to Admin/Authorised.
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.GroupItems;

[TestFixture]
public class GroupItemsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/GroupItems";

    [TearDown]
    public new void BaseTearDown() { base.BaseTearDown(); }

    // ── GET ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Post_WithoutToken_Returns401()
    {
        var payload = new GroupItemResource { GroupId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Post_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new GroupItemResource { GroupId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── DELETE by id ─────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Delete_UnknownId_WithAdminRole_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE remove by client and group ────────────────────────────────────

    [Test]
    public async Task RemoveByClientAndGroup_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync(
            $"{BaseRoute}/remove?clientId={Guid.NewGuid()}&groupId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemoveByClientAndGroup_WithAdminRole_WithUnknownIds_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/remove?clientId={Guid.NewGuid()}&groupId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
