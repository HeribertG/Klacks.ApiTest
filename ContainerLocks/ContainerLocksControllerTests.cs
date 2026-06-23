// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ContainerLocksController (api/backend/ContainerLocks).
 * Covers auth enforcement (401), Acquire, Heartbeat, and Release.
 * ContainerLocks use JWT auth only (no role restriction).
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.ContainerLocks;

[TestFixture]
public class ContainerLocksControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ContainerLocks";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

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
            ResourceType = "Shift",
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
            ResourceType = "Shift",
            ResourceId = Guid.NewGuid(),
            InstanceId = $"{TestPrefix}inst-acquire",
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Acquire", payload);
        var result = await response.Content.ReadFromJsonAsync<ContainerLockResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.ResourceType.ShouldBe("Shift");
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
