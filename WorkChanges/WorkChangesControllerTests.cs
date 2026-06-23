// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for WorkChangesController (api/backend/Works/Changes).
 * Covers list, get, post — including auth enforcement (401/404).
 * WorkChangesController inherits BaseController (JWT required) but has no role restrictions;
 * all authenticated users may call POST/PUT/DELETE.
 * POST requires a valid Work (Client+Shift), so creation tests are skipped here.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.WorkChanges;

[TestFixture]
public class WorkChangesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Works/Changes";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetWorkChangesList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostWorkChange_WithoutToken_Returns401()
    {
        var payload = new WorkChangeResource
        {
            WorkId = Guid.NewGuid(),
            Type = WorkChangeType.CorrectionEnd,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
        };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteWorkChange_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetWorkChangesList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<WorkChangeResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetWorkChangesList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetWorkChange_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteWorkChange_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
