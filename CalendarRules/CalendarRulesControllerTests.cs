// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for CalendarRulesController (api/backend/CalendarRules).
 * All endpoints require the Admin role; covers auth enforcement (401/403) and CRUD happy paths.
 */

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.ApiTest.CalendarRules;

[TestFixture]
public class CalendarRulesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/CalendarRules";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.CalendarRule
            .Where(r => r.Country == TestPrefix);
        DbContext.CalendarRule.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarRuleList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetCalendarRuleList");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetCalendarRuleList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetCalendarRuleList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetCalendarRuleList_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/GetCalendarRuleList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarRuleList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetCalendarRuleList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<CalendarRule>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetCalendarRule_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/CalendarRule/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET token list ──────────────────────────────────────────────────────

    [Test]
    public async Task GetRuleTokenList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetRuleTokenList?isSelected=false");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST CalendarRule ────────────────────────────────────────────────────

    [Test]
    public async Task PostCalendarRule_WithoutToken_Returns401()
    {
        var payload = MinimalCalendarRule();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/CalendarRule", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostCalendarRule_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalCalendarRule();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/CalendarRule", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostCalendarRule_WithAdminRole_ReturnsCreatedRule()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalCalendarRule();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/CalendarRule", payload);
        var created = await response.Content.ReadFromJsonAsync<CalendarRule>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Country.ShouldBe(TestPrefix);
    }

    // ── DELETE CalendarRule ──────────────────────────────────────────────────

    [Test]
    public async Task DeleteCalendarRule_WithAdminRole_ReturnsDeletedRule()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateCalendarRuleAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/CalendarRule/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<CalendarRule>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteCalendarRule_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/CalendarRule/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE cache endpoints ───────────────────────────────────────────────

    [Test]
    public async Task InvalidateHolidayCache_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/InvalidateHolidayCache/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task InvalidateAllHolidayCache_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/InvalidateAllHolidayCache");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private CalendarRuleResource MinimalCalendarRule() => new()
    {
        Country = TestPrefix,
        Rule = "fixed:01-01",
        State = string.Empty,
        SubRule = string.Empty,
        IsMandatory = true,
        IsPaid = true,
        Name = new Api.Domain.Common.MultiLanguage { De = "Test", En = "Test" },
    };

    private async Task<CalendarRule> CreateCalendarRuleAsync()
    {
        var payload = MinimalCalendarRule();
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/CalendarRule", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CalendarRule>())!;
    }
}
