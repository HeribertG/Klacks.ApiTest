// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AgentMemoriesController (api/backend/assistant/agents/{id}/memories).
 * Covers auth enforcement (401 without token) and basic CRUD response shapes.
 */

namespace Klacks.ApiTest.AgentMemories;

[TestFixture]
public class AgentMemoriesControllerTests : ApiTestBase
{
    private static readonly Guid KnownAgentId = Guid.NewGuid();
    private const string AgentsBaseRoute = "/api/backend/assistant/agents";

    private string MemoriesRoute(Guid agentId) => $"{AgentsBaseRoute}/{agentId}/memories";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMemories_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(MemoriesRoute(KnownAgentId));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateMemory_WithoutToken_Returns401()
    {
        var payload = new { Key = "k", Content = "v", Category = "general", Importance = 1, IsPinned = false };

        var response = await Client.PostAsJsonAsync(MemoriesRoute(KnownAgentId), payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateMemory_WithoutToken_Returns401()
    {
        var payload = new { Key = "k", Content = "v", Category = "general", Importance = 1, IsPinned = false };

        var response = await Client.PutAsJsonAsync($"{MemoriesRoute(KnownAgentId)}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteMemory_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{MemoriesRoute(KnownAgentId)}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task TogglePin_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            $"{MemoriesRoute(KnownAgentId)}/{Guid.NewGuid()}/pin", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET memories ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetMemories_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(MemoriesRoute(KnownAgentId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT update unknown memory ─────────────────────────────────────────────

    [Test]
    public async Task UpdateMemory_WithUserRole_UnknownMemory_Returns404()
    {
        AuthorizeAs(Roles.User);
        var payload = new { Key = "k", Content = "v", Category = "general", Importance = 1, IsPinned = false };

        var response = await Client.PutAsJsonAsync(
            $"{MemoriesRoute(KnownAgentId)}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Toggle pin unknown memory ─────────────────────────────────────────────

    [Test]
    public async Task TogglePin_WithUserRole_UnknownMemory_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(
            $"{MemoriesRoute(KnownAgentId)}/{Guid.NewGuid()}/pin", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
