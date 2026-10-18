// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for ContainerLocksController (api/backend/ContainerLocks).
 * Covers auth enforcement (401), Acquire, Heartbeat, and Release.
 * Acquire accepts only the resource types of ContainerLockResourceTypes (unknown type = 400) and, for the container
 * types, only an existing and visible container (missing or hidden = 404). ContainerWork needs an existing visible work.
 * ContainerLocks use JWT auth only (no role restriction).
 * @param TestPrefix - Prefix of every lock instance id and shift name this fixture creates; teardown deletes only rows carrying it
 * @param _customerId - Customer every container order must name (ClientlessOrderRules)
 * @param _containerId - Id of the OriginalShift copy of the sealed container order created in the fixture setup
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.ApiTest.ContainerLocks;

[TestFixture]
public class ContainerLocksControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ContainerLocks";
    private const string UnknownResourceType = "Shift";
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
            FirstName = "Lock",
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

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.ContainerLock.Where(c => c.InstanceId.StartsWith(TestPrefix));
        DbContext.ContainerLock.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task AcquireLock_WithoutToken_Returns401()
    {
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = ContainerLockResourceTypes.ContainerTemplate,
            ResourceId = Guid.NewGuid(),
            InstanceId = $"{TestPrefix}inst",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Acquire ─────────────────────────────────────────────────────────────

    [Test]
    public async Task AcquireLock_WithUserRole_ReturnsLockResource()
    {
        AuthorizeAs(Roles.User);
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = ContainerLockResourceTypes.ContainerTemplate,
            ResourceId = _containerId,
            InstanceId = $"{TestPrefix}inst-acquire",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);
        var result = await response.Content.ReadFromJsonAsync<ContainerLockResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.ResourceType.ShouldBe(ContainerLockResourceTypes.ContainerTemplate);
        result.ResourceId.ShouldBe(_containerId);
        result.Acquired.ShouldBeTrue();
    }

    [Test]
    public async Task AcquireLock_UnknownResourceType_Returns400()
    {
        AuthorizeAs(Roles.User);
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = UnknownResourceType,
            ResourceId = _containerId,
            InstanceId = $"{TestPrefix}inst-badtype",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AcquireLock_UnknownContainer_Returns404()
    {
        AuthorizeAs(Roles.User);
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = ContainerLockResourceTypes.ContainerShiftOverride,
            ResourceId = Guid.NewGuid(),
            InstanceId = $"{TestPrefix}inst-missing",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AcquireLock_UnknownContainer_AsAdmin_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = ContainerLockResourceTypes.ContainerTemplate,
            ResourceId = Guid.NewGuid(),
            InstanceId = $"{TestPrefix}inst-missing-admin",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AcquireLock_UnknownWork_Returns404()
    {
        AuthorizeAs(Roles.User);
        var payload = new AcquireContainerLockRequest
        {
            ResourceType = ContainerLockResourceTypes.ContainerWork,
            ResourceId = Guid.NewGuid(),
            InstanceId = $"{TestPrefix}inst-nowork",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Heartbeat ───────────────────────────────────────────────────────────

    [Test]
    public async Task Heartbeat_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/Heartbeat/{Guid.NewGuid()}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Release ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ReleaseLock_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ReleaseLock_UnknownId_ReturnsOkWithFalse()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");
        var released = await response.Content.ReadFromJsonAsync<bool>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        released.ShouldBe(false);
    }
}
