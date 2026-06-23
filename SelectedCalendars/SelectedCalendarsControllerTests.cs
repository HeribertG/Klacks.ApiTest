// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SelectedCalendarsController (api/backend/SelectedCalendars).
 * Covers get, post, delete — including auth enforcement (401/403/404).
 * SelectedCalendarsController inherits InputBaseController, restricting mutations to Admin/Authorised.
 * There is no parameterless list GET; a parent CalendarSelection is created once per fixture.
 * SelectedCalendars are cleaned up by CalendarSelectionId.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.SelectedCalendars;

[TestFixture]
public class SelectedCalendarsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/SelectedCalendars";
    private const string CalendarSelectionsRoute = "/api/backend/CalendarSelections";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _calendarSelectionId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var calendarPayload = new CalendarSelectionResource
        {
            Name = $"{TestPrefix}CalSel",
        };

        var response = await Client.PostAsJsonAsync(CalendarSelectionsRoute, calendarPayload);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<CalendarSelectionResource>())!;
        _calendarSelectionId = created.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        AuthorizeAs(Roles.Admin);

        if (_calendarSelectionId != Guid.Empty)
            await Client.DeleteAsync($"{CalendarSelectionsRoute}/{_calendarSelectionId}");
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.SelectedCalendar.Where(s => s.CalendarSelectionId == _calendarSelectionId && !s.IsDeleted);
        DbContext.SelectedCalendar.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostSelectedCalendar_WithoutToken_Returns401()
    {
        var payload = MinimalSelectedCalendar();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostSelectedCalendar_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalSelectedCalendar();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteSelectedCalendar_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteSelectedCalendar_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSelectedCalendar_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostSelectedCalendar_WithAdminRole_ReturnsCreatedSelectedCalendar()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalSelectedCalendar();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<SelectedCalendarResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.CalendarSelectionId.ShouldBe(_calendarSelectionId);
    }

    [Test]
    public async Task PostSelectedCalendar_WithAuthorisedRole_ReturnsCreatedSelectedCalendar()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalSelectedCalendar();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteSelectedCalendar_WithAdminRole_ReturnsDeletedSelectedCalendar()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateSelectedCalendarAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<SelectedCalendarResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteSelectedCalendar_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private SelectedCalendarResource MinimalSelectedCalendar() => new()
    {
        CalendarSelectionId = _calendarSelectionId,
        Country = "CH",
        State = string.Empty,
    };

    private async Task<SelectedCalendarResource> CreateSelectedCalendarAsync()
    {
        var payload = MinimalSelectedCalendar();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SelectedCalendarResource>())!;
    }
}
