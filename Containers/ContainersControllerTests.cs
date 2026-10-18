// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for ContainersController (api/backend/Containers).
 * Covers auth enforcement (401), GET available-tasks parameter validation (400),
 * GET templates and overrides for existing and unknown containers, and override endpoints.
 * A missing or hidden container is answered with 404 ("hidden = missing"); an existing, visible container
 * without templates or overrides still answers 200 with an empty list.
 * ContainersController uses JWT auth only (no role restriction).
 * @param TestPrefix - Name prefix of every shift this fixture creates; teardown deletes only rows carrying it
 * @param _customerId - Customer every container order must name (ClientlessOrderRules)
 * @param _containerId - Id of the OriginalShift copy of the sealed container order created in the fixture setup
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.ApiTest.Containers;

[TestFixture]
public class ContainersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Containers";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _customerId;
    private Guid _containerId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var customerPayload = new ClientResource
        {
            Name = $"{TestPrefix}Customer",
            FirstName = "Container",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var customerResponse = await Client.PostAsJsonAsync("/api/backend/Clients", customerPayload);
        customerResponse.EnsureSuccessStatusCode();
        _customerId = (await customerResponse.Content.ReadFromJsonAsync<ClientResource>())!.Id;

        var containerPayload = new ShiftResource
        {
            Name = $"{TestPrefix}Container",
            Abbreviation = "CNT",
            ClientId = _customerId,
            Status = ShiftStatus.SealedOrder,
            ShiftType = ShiftType.IsContainer,
            FromDate = new DateOnly(2026, 1, 1),
            StartShift = new TimeOnly(8, 0),
            EndShift = new TimeOnly(16, 0),
            WorkTime = 8,
            IsMonday = true,
            IsTuesday = true,
            IsWednesday = true,
            IsThursday = true,
            IsFriday = true,
        };
        var containerResponse = await Client.PostAsJsonAsync("/api/backend/Shifts", containerPayload);
        containerResponse.EnsureSuccessStatusCode();
        var container = (await containerResponse.Content.ReadFromJsonAsync<ShiftResource>())!;
        container.Status.ShouldBe(ShiftStatus.OriginalShift);
        _containerId = container.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var stale = context.Shift.Where(s => s.Name.StartsWith(TestPrefix));
            context.Shift.RemoveRange(stale);
            await context.SaveChangesAsync();
        }

        AuthorizeAs(Roles.Admin);
        if (_customerId != Guid.Empty)
        {
            await Client.DeleteAsync($"/api/backend/Clients/{_customerId}");
        }
    }

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

    [Test]
    public async Task GetAvailableTasks_UnknownContainerId_ReturnsNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(
            $"{BaseRoute}/available-tasks?containerId={Guid.NewGuid()}&weekday=1&fromTime=08:00&untilTime=16:00");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET templates ────────────────────────────────────────────────────────

    [Test]
    public async Task GetTemplates_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/templates");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTemplates_UnknownContainerId_ReturnsNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/templates");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetTemplates_UnknownContainerId_AsAdmin_ReturnsNotFound()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}/templates");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetTemplates_ExistingContainerWithoutTemplates_ReturnsOkWithEmptyList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{_containerId}/templates");
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
            $"{BaseRoute}/{_containerId}/overrides?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ContainerShiftOverrideResource>>();
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetOverridesForRange_UnknownContainerId_ReturnsNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/overrides?from=2026-01-01&to=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
