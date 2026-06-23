// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for WizardController (api/backend/Wizard).
 * Covers auth enforcement (401) and minimal-payload responses for Start, Cancel,
 * Apply (unknown job → 404), and ApplyAsScenario (unknown job → 404).
 * WizardController inherits BaseController (JWT-required, no role restriction).
 */

namespace Klacks.ApiTest.Wizard;

[TestFixture]
public class WizardControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Wizard";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Start_WithoutToken_Returns401()
    {
        var payload = MinimalStartRequest();

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
    public async Task Apply_WithoutToken_Returns401()
    {
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Apply", payload);

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
        var payload = MinimalStartRequest();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);
        var body = await response.Content.ReadFromJsonAsync<StartWizardResponse>();

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
        var body = await response.Content.ReadFromJsonAsync<CancelWizardResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Cancelled.ShouldBeFalse();
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Apply_UnknownJobId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Apply", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object MinimalStartRequest() => new
    {
        PeriodFrom = new DateOnly(2026, 1, 1),
        PeriodUntil = new DateOnly(2026, 1, 31),
        AgentIds = Array.Empty<Guid>(),
        ShiftIds = Array.Empty<Guid>(),
        AnalyseToken = (Guid?)null,
        TrainingOverrides = (object?)null,
        AgentOrderIsUserDefined = false
    };
}

file sealed record StartWizardResponse(Guid JobId);
file sealed record CancelWizardResponse(bool Cancelled);
