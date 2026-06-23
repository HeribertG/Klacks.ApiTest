// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ShiftsController (api/backend/Shifts).
 * Covers create, get, and delete — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.Shifts;

[TestFixture]
public class ShiftsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Shifts";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Shift
            .Where(s => s.Name.StartsWith(TestPrefix) && !s.IsDeleted);
        DbContext.Shift.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostShift_WithoutToken_Returns401()
    {
        var payload = MinimalShift($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostShift_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalShift($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetShift_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostShift_WithAdminRole_ReturnsCreatedShift()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalShift($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ShiftResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task PostShift_WithAuthorisedRole_ReturnsCreatedShift()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalShift($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteShift_WithAdminRole_ReturnsDeletedShift()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateShiftAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<ShiftResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteShift_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ShiftResource MinimalShift(string name) => new()
    {
        Name = name,
        Abbreviation = "TST",
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

    private async Task<ShiftResource> CreateShiftAsync(string name)
    {
        var payload = MinimalShift(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShiftResource>())!;
    }
}
