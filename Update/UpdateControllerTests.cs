// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for UpdateController (api/backend/Update).
 * Covers status, history, config, trigger, rollback, and cancel — including
 * auth enforcement (401 for unauthenticated, 403 for non-admin roles).
 */

using Klacks.Api.Application.DTOs.Update;

namespace Klacks.ApiTest.Update;

[TestFixture]
public class UpdateControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Update";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    // ── Auth: unauthenticated → 401 ─────────────────────────────────────────

    [Test]
    public async Task GetStatus_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/Status");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetHistory_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/History");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetConfig_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/Config");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PutConfig_WithoutToken_Returns401()
    {
        var payload = new UpdateConfig();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/Config", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostTrigger_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/Trigger", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostRollback_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/Rollback", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostCancel_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Auth: non-admin role → 403 ───────────────────────────────────────────

    [Test]
    public async Task GetStatus_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/Status");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetHistory_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/History");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetConfig_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/Config");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostTrigger_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/Trigger", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostTrigger_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsync($"{BaseRoute}/Trigger", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET Status (Admin) ───────────────────────────────────────────────────

    [Test]
    public async Task GetStatus_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/Status");
        var result = await response.Content.ReadFromJsonAsync<UpdateStatusResult>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.CurrentVersion.ShouldNotBeNull();
    }

    // ── GET History (Admin) ──────────────────────────────────────────────────

    [Test]
    public async Task GetHistory_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/History");
        var items = await response.Content.ReadFromJsonAsync<IReadOnlyList<UpdateHistoryItem>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        items.ShouldNotBeNull();
    }

    [Test]
    public async Task GetHistory_WithTakeParameter_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/History?take=5");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET Config (Admin) ───────────────────────────────────────────────────

    [Test]
    public async Task GetConfig_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/Config");
        var config = await response.Content.ReadFromJsonAsync<UpdateConfig>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        config.ShouldNotBeNull();
    }

    // ── PUT Config (Admin) ───────────────────────────────────────────────────

    [Test]
    public async Task PutConfig_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);
        var getResponse = await Client.GetAsync($"{BaseRoute}/Config");
        var existing = await getResponse.Content.ReadFromJsonAsync<UpdateConfig>();
        existing.ShouldNotBeNull();
        existing!.NotifyOnly = !existing.NotifyOnly;

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/Config", existing);
        var updated = await response.Content.ReadFromJsonAsync<UpdateConfig>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.NotifyOnly.ShouldBe(existing.NotifyOnly);
    }

    // ── POST Cancel (Admin) ──────────────────────────────────────────────────

    [Test]
    public async Task PostCancel_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
