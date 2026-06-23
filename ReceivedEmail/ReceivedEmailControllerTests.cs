// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ReceivedEmailController (api/backend/ReceivedEmail).
 * Authenticated endpoint — any valid role is accepted (no admin restriction).
 * Tests cover 401 enforcement, 404 for unknown resources, and reachability of list endpoints.
 * Avoids FetchNow and TestImapConnection to prevent external IMAP side effects.
 */

namespace Klacks.ApiTest.ReceivedEmail;

[TestFixture]
public class ReceivedEmailControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ReceivedEmail";

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/List");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetList_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/List");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── UnreadCount ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetUnreadCount_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/UnreadCount");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUnreadCount_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/UnreadCount");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GroupTree ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroupTree_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GroupTree");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetGroupTree_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GroupTree");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetById_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── ByGroup ───────────────────────────────────────────────────────────────

    [Test]
    public async Task GetByGroup_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ByGroup/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── MarkAsRead ────────────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsRead_WithoutToken_Returns401()
    {
        var response = await Client.PutAsync($"{BaseRoute}/{Guid.NewGuid()}/Read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
