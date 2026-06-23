// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SchedulingRulesController (api/backend/SchedulingRules).
 * Covers list, get, post, delete — including auth enforcement (401/403/404).
 * SchedulingRulesController inherits InputBaseController, restricting mutations to Admin/Authorised.
 */

using Klacks.Api.Application.DTOs.Scheduling;

namespace Klacks.ApiTest.SchedulingRules;

[TestFixture]
public class SchedulingRulesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/SchedulingRules";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.SchedulingRules.Where(r => !r.IsDeleted && r.Name.StartsWith(TestPrefix));
        DbContext.SchedulingRules.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSchedulingRules_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostSchedulingRule_WithoutToken_Returns401()
    {
        var payload = MinimalSchedulingRule($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostSchedulingRule_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalSchedulingRule($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteSchedulingRule_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteSchedulingRule_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSchedulingRules_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<SchedulingRuleResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSchedulingRule_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostSchedulingRule_WithAdminRole_ReturnsCreatedSchedulingRule()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalSchedulingRule($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<SchedulingRuleResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe(payload.Name);
    }

    [Test]
    public async Task PostSchedulingRule_WithAuthorisedRole_ReturnsCreatedSchedulingRule()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalSchedulingRule($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteSchedulingRule_WithAdminRole_ReturnsDeletedSchedulingRule()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateSchedulingRuleAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<SchedulingRuleResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteSchedulingRule_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static SchedulingRuleResource MinimalSchedulingRule(string name) => new()
    {
        Name = name,
    };

    private async Task<SchedulingRuleResource> CreateSchedulingRuleAsync(string name)
    {
        var payload = MinimalSchedulingRule(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SchedulingRuleResource>())!;
    }
}
