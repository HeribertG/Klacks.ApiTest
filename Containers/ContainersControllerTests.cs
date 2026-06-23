// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ContainersController (api/backend/Containers).
 * Covers auth enforcement (401), GET available-tasks parameter validation (400),
 * GET templates for unknown container, and override endpoints.
 * ContainersController uses JWT auth only (no role restriction).
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.Containers;

[TestFixture]
public class ContainersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Containers";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAvailableTasks_WithoutToken_Returns401()
    {
        var containerId = Guid.NewGuid();
        var response = await Client.GetAsync(
            $"{BaseRoute}/available-tasks?containerId={containerId}&weekday=1&fromTime=08:00&untilTime=16:00");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET available-tasks validation ──────────────────────────────────────

    [Test]
    public async Task GetAvailableTasks_InvalidFromTime_Returns400()
    {
        AuthorizeAs(Roles.User);
        var containerId = Guid.NewGuid();

        var response = await Client.GetAsync(
            $"{BaseRoute}/available-tasks?containerId={containerId}&weekday=1&fromTime=notatime&untilTime=16:00");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAvailableTasks_InvalidUntilTime_Returns400()
    {
        AuthorizeAs(Roles.User);
        var containerId = Guid.NewGuid();

        var response = await Client.GetAsync(
            $"{BaseRoute}/available-tasks?containerId={containerId}&weekday=1&fromTime=08:00&untilTime=notatime");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── GET templates ────────────────────────────────────────────────────────

    [Test]
    public async Task GetTemplates_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/templates");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTemplates_UnknownContainerId_ReturnsOkWithEmptyList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/templates");
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ContainerTemplateResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.ShouldBeEmpty();
    }

    // ── GET overrides ────────────────────────────────────────────────────────

    [Test]
    public async Task GetOverrideForDate_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/overrides/2026-06-20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOverrideForDate_UnknownContainerAndDate_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/overrides/2026-06-20");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetOverridesForRange_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/overrides?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ContainerShiftOverrideResource>>();
        result.ShouldNotBeNull();
    }

    // ── DELETE override ───────────────────────────────────────────────────────

    [Test]
    public async Task DeleteOverride_UnknownIds_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/overrides/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
