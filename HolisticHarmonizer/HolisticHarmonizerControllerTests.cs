// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for HolisticHarmonizerController (api/backend/HolisticHarmonizer).
 * Covers auth enforcement (401) and happy-path responses for Start, Cancel,
 * CheckAllModels, and ApplyAsScenario endpoints.
 * HolisticHarmonizerController inherits BaseController (JWT-required, no role restriction).
 */

namespace Klacks.ApiTest.HolisticHarmonizer;

[TestFixture]
public class HolisticHarmonizerControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/HolisticHarmonizer";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Start_WithoutToken_Returns401()
    {
        var payload = new
        {
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodUntil = new DateOnly(2026, 1, 31),
            AgentIds = Array.Empty<Guid>(),
            AnalyseToken = (Guid?)null,
            Language = (string?)null
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Cancel_WithoutToken_Returns401()
    {
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CheckAllModels_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/CheckAllModels", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApplyAsScenario_WithoutToken_Returns401()
    {
        var payload = new { JobId = Guid.NewGuid(), GroupId = (Guid?)null };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/ApplyAsScenario", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Start ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Start_WithUserRole_ReturnsJobId()
    {
        AuthorizeAs(Roles.User);
        var payload = new
        {
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodUntil = new DateOnly(2026, 1, 31),
            AgentIds = Array.Empty<Guid>(),
            AnalyseToken = (Guid?)null,
            Language = "en"
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);
        var body = await response.Content.ReadFromJsonAsync<StartHolisticHarmonizerResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.JobId.ShouldNotBe(Guid.Empty);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Cancel_UnknownJobId_ReturnsFalse()
    {
        AuthorizeAs(Roles.User);
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);
        var body = await response.Content.ReadFromJsonAsync<CancelHolisticHarmonizerResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Cancelled.ShouldBeFalse();
    }

    // ── CheckAllModels ────────────────────────────────────────────────────────

    [Test]
    public async Task CheckAllModels_WithUserRole_ReturnsModelList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/CheckAllModels", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── ApplyAsScenario ──────────────────────────────────────────────────────

    [Test]
    public async Task ApplyAsScenario_UnknownJobId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new { JobId = Guid.NewGuid(), GroupId = (Guid?)null };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/ApplyAsScenario", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

file sealed record StartHolisticHarmonizerResponse(Guid JobId);
file sealed record CancelHolisticHarmonizerResponse(bool Cancelled);
