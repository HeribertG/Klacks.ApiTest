// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for CalendarSelectionsController (api/backend/CalendarSelections).
 * Covers list, get, post, delete — including auth enforcement (401/403/404).
 * CalendarSelectionsController inherits InputBaseController, restricting mutations to Admin/Authorised.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.CalendarSelections;

[TestFixture]
public class CalendarSelectionsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/CalendarSelections";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.CalendarSelection.Where(c => !c.IsDeleted && c.Name.StartsWith(TestPrefix));
        DbContext.CalendarSelection.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarSelections_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostCalendarSelection_WithoutToken_Returns401()
    {
        var payload = MinimalCalendarSelection($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostCalendarSelection_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalCalendarSelection($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteCalendarSelection_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteCalendarSelection_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarSelections_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<CalendarSelectionResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetUsedByContracts_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/used-by-contracts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarSelection_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostCalendarSelection_WithAdminRole_ReturnsCreatedCalendarSelection()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalCalendarSelection($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<CalendarSelectionResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe(payload.Name);
    }

    [Test]
    public async Task PostCalendarSelection_WithAuthorisedRole_ReturnsCreatedCalendarSelection()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalCalendarSelection($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteCalendarSelection_WithAdminRole_ReturnsDeletedCalendarSelection()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateCalendarSelectionAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<CalendarSelectionResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteCalendarSelection_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static CalendarSelectionResource MinimalCalendarSelection(string name) => new()
    {
        Name = name,
    };

    private async Task<CalendarSelectionResource> CreateCalendarSelectionAsync(string name)
    {
        var payload = MinimalCalendarSelection(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarSelectionResource>())!;
    }
}
