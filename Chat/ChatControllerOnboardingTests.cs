// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests of the per-account intro tour over the real container: a planner without the admin role gets the
 * intro slice in the welcome payload, may write their own intro state, and is refused (403, never 401) when writing
 * the setup tour; the "I already know Klacks" endpoint requires a token.
 */

using System.Text.Json;

namespace Klacks.ApiTest.Chat;

[TestFixture]
public class ChatControllerOnboardingTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/assistant/chat";

    [Test]
    public async Task Welcome_ForAPlanner_CarriesTheIntroSlice_OfferedToTheNewAccount()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/welcome?lang=en&localHour=9&weekday=5&isReopen=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var onboarding = (await ReadJsonAsync(response)).GetProperty("onboarding");
        onboarding.GetProperty("kind").GetString().ShouldBe("intro");
        onboarding.GetProperty("status").GetString().ShouldBe(IntroTourStatus.Pending);
        onboarding.GetProperty("shouldOffer").GetBoolean().ShouldBeTrue();
        onboarding.GetProperty("isLegacyOffer").GetBoolean().ShouldBeFalse();
    }

    [Test]
    public async Task SaveIntroState_ForAPlanner_WritesTheirOwnState()
    {
        AuthorizeAs(Roles.User);

        var offered = await Client.PostAsJsonAsync($"{BaseRoute}/onboarding/state", new { kind = "intro", status = "offered" });
        offered.StatusCode.ShouldBe(HttpStatusCode.OK);

        var started = await Client.PostAsJsonAsync(
            $"{BaseRoute}/onboarding/state", new { kind = "intro", status = "in_progress", completedStation = "schedule" });

        started.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(started);
        body.GetProperty("kind").GetString().ShouldBe("intro");
        body.GetProperty("status").GetString().ShouldBe(IntroTourStatus.InProgress);
        body.GetProperty("showCard").GetBoolean().ShouldBeTrue();
        body.GetProperty("completedStations").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "schedule" });
    }

    [Test]
    public async Task SaveIntroState_WithAnUnknownStatus_Returns400()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/onboarding/state", new { kind = "intro", status = "pending" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SaveSetupState_ForAPlanner_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/onboarding/state", new { kind = "setup", status = "completed" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SetAdvancedExplainDepth_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/onboarding/explain-depth/advanced", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
