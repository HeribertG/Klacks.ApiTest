// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ChatController (api/backend/assistant/chat).
 * Covers authorization enforcement (401) for all endpoints.
 */

namespace Klacks.ApiTest.Chat;

[TestFixture]
public class ChatControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/chat";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Chat_Post_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_PostStream_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/stream", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_GetFunctions_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/functions");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_GetFunctionDefinitions_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/function-definitions");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_PostExecuteFunction_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/execute-function", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_PostExecuteFunctionsBatch_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/execute-functions-batch", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_GetWarmup_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/warmup");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_GetWelcome_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/welcome");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_PostOnboardingState_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/onboarding/state", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Chat_GetHelp_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/help");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
