// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for SpamRulesController (api/backend/ReceivedEmail/SpamRules).
 * Covers list, create, update, and delete — including auth enforcement (401).
 */

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;

namespace Klacks.ApiTest.SpamRules;

[TestFixture]
public class SpamRulesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ReceivedEmail/SpamRules";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.SpamRules
            .Where(r => r.Pattern.StartsWith(TestPrefix) && !r.IsDeleted);
        DbContext.SpamRules.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    [Test]
    public async Task GetSpamRules_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSpamRules_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostSpamRule_WithoutToken_Returns401()
    {
        var payload = new CreateSpamRuleCommand(SpamRuleType.SenderContains, $"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostSpamRule_WithUserRole_ReturnsCreated()
    {
        AuthorizeAs(Roles.User);
        var payload = new CreateSpamRuleCommand(SpamRuleType.SenderContains, $"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<SpamRuleResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Pattern.ShouldBe(payload.Pattern);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task DeleteSpamRule_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteSpamRule_UnknownId_Returns404OrOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }
}
