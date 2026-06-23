// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentsController (api/backend/assistant/agents).
 * Covers auth enforcement (401 without token), basic CRUD responses, and sub-resource endpoints.
 */

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.ApiTest.Agents;

[TestFixture]
public class AgentsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/agents";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Agents
            .Where(a => a.Name.StartsWith(TestPrefix) && !a.IsDeleted);
        DbContext.Agents.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        var payload = new CreateAgentRequest($"{TestPrefix}Unauth", null, null);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET all ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET by id ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetById_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithUserRole_ReturnsCreated()
    {
        AuthorizeAs(Roles.User);
        var payload = new CreateAgentRequest($"{TestPrefix}Create", "Test Agent", null);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);
        var payload = new UpdateAgentRequest("Updated Name", null, null, true);

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET skills ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSkills_UnknownAgentId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/skills");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET sessions ────────────────────────────────────────────────────────

    [Test]
    public async Task GetSessions_UnknownAgentId_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/sessions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
