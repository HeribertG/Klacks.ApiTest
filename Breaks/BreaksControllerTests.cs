// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for BreaksController (api/backend/Breaks).
 * Covers list, get, post, put, delete, confirm/unconfirm — including auth enforcement (401/404).
 * A Client, Shift, and Absence are created once per fixture for use as FK dependencies.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Common;

namespace Klacks.ApiTest.Breaks;

[TestFixture]
public class BreaksControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Breaks";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _clientId;
    private Guid _shiftId;
    private Guid _absenceId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var clientPayload = new ClientResource
        {
            Name = $"{TestPrefix}Client",
            FirstName = "Break",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var clientResponse = await Client.PostAsJsonAsync("/api/backend/Clients", clientPayload);
        clientResponse.EnsureSuccessStatusCode();
        _clientId = (await clientResponse.Content.ReadFromJsonAsync<ClientResource>())!.Id;

        var shiftPayload = new ShiftResource
        {
            Name = $"{TestPrefix}Shift",
            Abbreviation = "BKT",
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
        _shiftId = (await shiftResponse.Content.ReadFromJsonAsync<ShiftResource>())!.Id;

        var absencePayload = new AbsenceResource
        {
            Name = new MultiLanguage { De = $"{TestPrefix}Absence", En = $"{TestPrefix}Absence" },
            Abbreviation = new MultiLanguage { De = "BRK", En = "BRK" },
            Description = new MultiLanguage { De = "Break absence", En = "Break absence" },
            Color = "#0000FF",
            DefaultLength = 1,
            DefaultValue = 1,
        };
        var absenceResponse = await Client.PostAsJsonAsync("/api/backend/Absences", absencePayload);
        absenceResponse.EnsureSuccessStatusCode();
        _absenceId = (await absenceResponse.Content.ReadFromJsonAsync<AbsenceResource>())!.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        AuthorizeAs(Roles.Admin);

        if (_absenceId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Absences/{_absenceId}");
        if (_clientId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Clients/{_clientId}");
        if (_shiftId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Shifts/{_shiftId}");
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Break.Where(b => b.ClientId == _clientId && !b.IsDeleted);
        DbContext.Break.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostBreak_WithoutToken_Returns401()
    {
        var payload = MinimalBreak();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetBreaks_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<BreakResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetBreak_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostBreak_WithAdminRole_ReturnsCreatedBreak()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalBreak();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<BreakResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(_clientId);
        created.AbsenceId.ShouldBe(_absenceId);
    }

    [Test]
    public async Task PostBreak_WithUserRole_ReturnsCreatedBreak()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalBreak();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutBreak_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalBreak();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PutBreak_WithAdminRole_ReturnsUpdatedBreak()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateBreakAsync();
        created.Information = "Updated break";

        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<BreakResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Information.ShouldBe("Updated break");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteBreak_WithAdminRole_ReturnsDeletedBreak()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateBreakAsync();
        var periodStart = created.CurrentDate.AddDays(-1);
        var periodEnd = created.CurrentDate.AddDays(30);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{created.Id}?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}");
        var deleted = await response.Content.ReadFromJsonAsync<BreakResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteBreak_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{Guid.NewGuid()}?periodStart=2026-01-01&periodEnd=2026-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Confirm / Unconfirm ─────────────────────────────────────────────────

    [Test]
    public async Task ConfirmBreak_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Confirm", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UnconfirmBreak_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/Unconfirm", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ConfirmBreak_WithAdminRole_ReturnsConfirmedBreak()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateBreakAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{created.Id}/Confirm", null);
        var confirmed = await response.Content.ReadFromJsonAsync<BreakResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        confirmed.ShouldNotBeNull();
        confirmed!.Id.ShouldBe(created.Id);
        confirmed.LockLevel.ShouldNotBe(WorkLockLevel.None);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private BreakResource MinimalBreak() => new()
    {
        ClientId = _clientId,
        AbsenceId = _absenceId,
        CurrentDate = new DateOnly(2026, 6, 17),
        StartTime = new TimeOnly(12, 0),
        EndTime = new TimeOnly(12, 30),
        WorkTime = 0.5m,
    };

    private async Task<BreakResource> CreateBreakAsync()
    {
        var payload = MinimalBreak();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BreakResource>())!;
    }
}
