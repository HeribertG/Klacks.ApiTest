// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for WorksController (api/backend/Works).
 * Covers get, post, put, delete, confirm/unconfirm — including auth enforcement (401/404).
 * A Client and a Shift are created once per fixture and cleaned up in OneTimeTearDown.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Works;

[TestFixture]
public class WorksControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Works";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _clientId;
    private Guid _shiftId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var clientPayload = new ClientResource
        {
            Name = $"{TestPrefix}Client",
            FirstName = "Work",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var clientResponse = await Client.PostAsJsonAsync("/api/backend/Clients", clientPayload);
        clientResponse.EnsureSuccessStatusCode();
        var createdClient = (await clientResponse.Content.ReadFromJsonAsync<ClientResource>())!;
        _clientId = createdClient.Id;

        var shiftPayload = new ShiftResource
        {
            Name = $"{TestPrefix}Shift",
            Abbreviation = "WTT",
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
        var shiftResponse = await Client.PostAsJsonAsync("/api/backend/Shifts", shiftPayload);
        shiftResponse.EnsureSuccessStatusCode();
        var createdShift = (await shiftResponse.Content.ReadFromJsonAsync<ShiftResource>())!;
        _shiftId = createdShift.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        AuthorizeAs(Roles.Admin);

        if (_clientId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Clients/{_clientId}");

        if (_shiftId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Shifts/{_shiftId}");
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Work.Where(w => w.ClientId == _clientId && !w.IsDeleted);
        DbContext.Work.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostWork_WithoutToken_Returns401()
    {
        var payload = MinimalWork();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetWork_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostWork_WithAdminRole_ReturnsCreatedWork()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalWork();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<WorkResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(_clientId);
        created.ShiftId.ShouldBe(_shiftId);
    }

    [Test]
    public async Task PostWork_WithUserRole_ReturnsCreatedWork()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalWork();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutWork_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalWork();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PutWork_WithAdminRole_ReturnsUpdatedWork()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateWorkAsync();
        created.Information = "Updated";

        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<WorkResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Information.ShouldBe("Updated");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteWork_WithAdminRole_ReturnsDeletedWork()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateWorkAsync();
        var periodStart = created.CurrentDate.AddDays(-1);
        var periodEnd = created.CurrentDate.AddDays(30);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{created.Id}?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}");
        var deleted = await response.Content.ReadFromJsonAsync<WorkResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteWork_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{Guid.NewGuid()}?periodStart=2026-01-01&periodEnd=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Confirm / Unconfirm ─────────────────────────────────────────────────

    [Test]
    public async Task ConfirmWork_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Confirm", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UnconfirmWork_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Unconfirm", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ConfirmWork_WithAdminRole_ReturnsConfirmedWork()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateWorkAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{created.Id}/Confirm", null);
        var confirmed = await response.Content.ReadFromJsonAsync<WorkResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        confirmed.ShouldNotBeNull();
        confirmed!.Id.ShouldBe(created.Id);
        confirmed.LockLevel.ShouldNotBe(WorkLockLevel.None);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private WorkResource MinimalWork() => new()
    {
        ClientId = _clientId,
        ShiftId = _shiftId,
        CurrentDate = new DateOnly(2026, 6, 16),
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(16, 0),
        WorkTime = 8,
    };

    private async Task<WorkResource> CreateWorkAsync()
    {
        var payload = MinimalWork();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkResource>())!;
    }
}
