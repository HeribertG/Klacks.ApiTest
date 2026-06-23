// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentSoulController (api/backend/assistant/agents/{id}/soul).
 * Covers auth enforcement (401 without token) and basic GET/PUT/DELETE/history response shapes.
 */

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.ApiTest.AgentSoul;

[TestFixture]
public class AgentSoulControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/agents";
    private const string SectionType = "personality";

    private static string SoulRoute(Guid agentId) => $"{BaseRoute}/{agentId}/soul";
    private static string SectionRoute(Guid agentId, string sectionType) => $"{BaseRoute}/{agentId}/soul/{sectionType}";
    private static string HistoryRoute(Guid agentId) => $"{BaseRoute}/{agentId}/soul/history";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSoulSections_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(SoulRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpsertSoulSection_WithoutToken_Returns401()
    {
        var payload = new UpsertSoulRequest("Test content", 1);

        var response = await Client.PutAsJsonAsync(SectionRoute(Guid.NewGuid(), SectionType), payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeactivateSoulSection_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync(SectionRoute(Guid.NewGuid(), SectionType));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSoulHistory_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(HistoryRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET soul sections ───────────────────────────────────────────────────

    [Test]
    public async Task GetSoulSections_WithUserRole_WithUnknownAgentId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(SoulRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT upsert ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpsertSoulSection_WithUserRole_IsNotUnauthorized()
    {
        AuthorizeAs(Roles.User);
        var payload = new UpsertSoulRequest("Test soul content", 1);

        var response = await Client.PutAsJsonAsync(SectionRoute(Guid.NewGuid(), SectionType), payload);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    // ── DELETE deactivate ───────────────────────────────────────────────────

    [Test]
    public async Task DeactivateSoulSection_WithUserRole_WithUnknownAgent_ReturnsNoContentOrNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync(SectionRoute(Guid.NewGuid(), SectionType));

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    // ── GET history ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetSoulHistory_WithUserRole_WithUnknownAgentId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(HistoryRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
