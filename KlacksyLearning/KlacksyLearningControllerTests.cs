// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for KlacksyLearningController (api/backend/assistant/learning), which replaced
 * the removed SkillProposalsController on 2026-08-30. The entire controller is Admin only; covers
 * 401/403 enforcement and the admin read of the learned phrases.
 */

namespace Klacks.ApiTest.KlacksyLearning;

[TestFixture]
public class KlacksyLearningControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/learning";

    [Test]
    public async Task GetPhrases_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/phrases");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetPhrases_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/phrases");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetPhrases_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/phrases");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetPhrases_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/phrases");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ApprovePhraseProposal_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/phrases/{Guid.NewGuid()}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApprovePhraseProposal_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/phrases/{Guid.NewGuid()}/approve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
