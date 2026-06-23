// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for KlacksyTrainingController (api/admin/klacksy-training).
 * Covers auth enforcement (401 without token) and role-based access (403 for non-Admin).
 */

namespace Klacks.ApiTest.KlacksyTraining;

[TestFixture]
public class KlacksyTrainingControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/admin/klacksy-training";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetTargets_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/targets");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTargets_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/targets");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateSynonyms_WithoutToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/targets/someTargetId/synonyms", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateSynonyms_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/targets/someTargetId/synonyms", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetFeedback_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/feedback?locale=de");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFeedback_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/feedback?locale=de");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
