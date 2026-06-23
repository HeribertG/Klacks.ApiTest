// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AutoWizardController (api/backend/AutoWizard).
 * Covers auth enforcement (401/403), the Admin-only role restriction, limit-validation
 * returning 400, and the Cancel endpoint for an unknown job.
 * AutoWizardController requires Admin role.
 */

namespace Klacks.ApiTest.AutoWizard;

[TestFixture]
public class AutoWizardControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/AutoWizard";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Start_WithoutToken_Returns401()
    {
        var payload = MinimalStartRequest();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Start_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalStartRequest();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Cancel_WithoutToken_Returns401()
    {
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Cancel_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Start — limit validation ─────────────────────────────────────────────

    [Test]
    public async Task Start_ExceedsAgentLimit_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var agentIds = Enumerable.Range(0, 600).Select(_ => Guid.NewGuid()).ToList();
        var payload = new
        {
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodUntil = new DateOnly(2026, 1, 31),
            AgentIds = agentIds,
            ShiftIds = (List<Guid>?)null,
            GroupId = (Guid?)null,
            AnalyseToken = (Guid?)null,
            Language = (string?)null
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Start — happy path ───────────────────────────────────────────────────

    [Test]
    public async Task Start_WithAdminRole_ReturnsJobId()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalStartRequest();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);
        var body = await response.Content.ReadFromJsonAsync<StartAutoWizardResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.JobId.ShouldNotBe(Guid.Empty);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Cancel_UnknownJobId_ReturnsFalse()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);
        var body = await response.Content.ReadFromJsonAsync<CancelAutoWizardResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Cancelled.ShouldBeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object MinimalStartRequest() => new
    {
        PeriodFrom = new DateOnly(2026, 1, 1),
        PeriodUntil = new DateOnly(2026, 1, 31),
        AgentIds = Array.Empty<Guid>(),
        ShiftIds = (List<Guid>?)null,
        GroupId = (Guid?)null,
        AnalyseToken = (Guid?)null,
        Language = (string?)null
    };
}

file sealed record StartAutoWizardResponse(Guid JobId);
file sealed record CancelAutoWizardResponse(bool Cancelled);
