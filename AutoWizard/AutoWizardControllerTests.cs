// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for AutoWizardController (api/backend/AutoWizard).
 * Covers auth enforcement (401), the opening to every planner (owner rule 2026-11-07: autofill is planning work, so a
 * signed-in user without the Admin role is no longer refused by role; group visibility bounds the start and the
 * job access rule answers a job the caller may not address like an unknown one), limit-validation returning 400,
 * and the Cancel endpoint for an unknown job.
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
    public async Task Start_WithUserRole_ForAnUnknownGroup_IsNotRefusedByRole()
    {
        AuthorizeAs(Roles.User);
        var payload = new
        {
            PeriodFrom = new DateOnly(2025, 3, 1),
            PeriodUntil = new DateOnly(2025, 3, 31),
            AgentIds = Array.Empty<Guid>(),
            ShiftIds = (List<Guid>?)null,
            GroupId = (Guid?)Guid.NewGuid(),
            AnalyseToken = (Guid?)null,
            Language = (string?)null
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Start", payload);

        // A restricted planner gets 404 (a group outside the scope is answered like a missing one); without any group
        // restriction in the database the planner is unrestricted and the run starts. Never 403: the role gate is gone.
        // Its own period keeps it clear of the January run of Start_WithAdminRole_ReturnsJobId (AutofillStartGuard 409).
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Test]
    public async Task Status_WithUserRole_UnknownJob_ReturnsUnknown()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/Status/{Guid.NewGuid()}");
        var body = await response.Content.ReadFromJsonAsync<AutoWizardJobStatusResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Status.ShouldBe("unknown");
    }

    [Test]
    public async Task Cancel_WithoutToken_Returns401()
    {
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Cancel_WithUserRole_UnknownJob_ReturnsFalse()
    {
        AuthorizeAs(Roles.User);
        var payload = new { JobId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Cancel", payload);
        var body = await response.Content.ReadFromJsonAsync<CancelAutoWizardResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body!.Cancelled.ShouldBeFalse();
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
file sealed record AutoWizardJobStatusResponse(string Status);
