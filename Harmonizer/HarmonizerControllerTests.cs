// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for HarmonizerController (api/backend/Harmonizer).
 * Covers auth enforcement (401) and minimal request validation for Start, Cancel,
 * and ApplyAsScenario endpoints. Start and Cancel always return 200; ApplyAsScenario
 * returns 404 for an unknown job id.
 */

namespace Klacks.ApiTest.Harmonizer;

[TestFixture]
public class HarmonizerControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Harmonizer";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Start_WithoutToken_Returns401()
    {
        var payload = new
        {
            PeriodFrom = "2026-01-01",
            PeriodUntil = "2026-01-31",
            AgentIds = Array.Empty<Guid>(),
            AnalyseToken = (Guid?)null
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
            AnalyseToken = (Guid?)null
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);
        var body = await response.Content.ReadFromJsonAsync<StartHarmonizerResponse>();

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
        var body = await response.Content.ReadFromJsonAsync<CancelHarmonizerResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Cancelled.ShouldBeFalse();
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

file sealed record StartHarmonizerResponse(Guid JobId);
file sealed record CancelHarmonizerResponse(bool Cancelled);
