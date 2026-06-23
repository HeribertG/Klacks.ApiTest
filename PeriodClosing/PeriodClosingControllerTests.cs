// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for PeriodClosingController (api/backend/PeriodClosing).
 * Covers auth enforcement (401/403), Admin-only role restriction for Seal/Unseal,
 * and GET endpoints returning 200 with valid date ranges.
 * All endpoints require Admin role.
 */

namespace Klacks.ApiTest.PeriodClosing;

[TestFixture]
public class PeriodClosingControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/PeriodClosing";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Seal_WithoutToken_Returns401()
    {
        var payload = new { StartDate = "2026-01-01", EndDate = "2026-01-31", GroupId = (Guid?)null, Reason = (string?)null };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Seal", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Seal_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31), GroupId = (Guid?)null, Reason = (string?)null };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Seal", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Unseal_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31), GroupId = (Guid?)null, Reason = (string?)null };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Unseal", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetSealedPeriods_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/SealedPeriods?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET SealedPeriods ────────────────────────────────────────────────────

    [Test]
    public async Task GetSealedPeriods_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/SealedPeriods?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetSealedPeriods_WithGroupFilter_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/SealedPeriods?from=2026-01-01&to=2026-12-31&groupId={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET UsedPeriods ──────────────────────────────────────────────────────

    [Test]
    public async Task GetUsedPeriods_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/UsedPeriods");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET Issues ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetIssues_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/Issues?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET AuditLog ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetAuditLog_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/AuditLog?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET ExportLog ────────────────────────────────────────────────────────

    [Test]
    public async Task GetExportLog_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/ExportLog?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Seal / Unseal ────────────────────────────────────────────────────────

    [Test]
    public async Task Seal_WithAdminRole_ReturnsAffectedCount()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new
        {
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 31),
            GroupId = (Guid?)null,
            Reason = "ApiTest seal"
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Seal", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Unseal_WithAdminRole_ReturnsAffectedCount()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new
        {
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 31),
            GroupId = (Guid?)null,
            Reason = "ApiTest unseal"
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Unseal", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
