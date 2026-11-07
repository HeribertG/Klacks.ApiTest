// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for WorksController (api/backend/Works).
 * Covers get, post (also repeated with the same Idempotency-Key), put, delete, confirm/unconfirm — including auth enforcement (401/404).
 * The manual period-hours recalculations admit planners; only the admin may confirm frozen periods (403 otherwise). Their
 * window lies in 1900, before every membership, so they write nothing into the shared database.
 * A Client and a Shift are created once per fixture and cleaned up in OneTimeTearDown.
 */

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Works;

[TestFixture]
public class WorksControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Works";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private static readonly DateOnly RecalculationStart = new(1900, 1, 1);
    private static readonly DateOnly RecalculationEnd = new(1900, 1, 31);

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
            ClientId = _clientId,
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

    // ── POST with Idempotency-Key ───────────────────────────────────────────

    [Test]
    public async Task PostWork_SameIdempotencyKeyTwice_CreatesOneWork_AndAnswersBothWithItsId()
    {
        AuthorizeAs(Roles.User);
        var key = Guid.NewGuid().ToString("D");

        var first = await PostWithIdempotencyKeyAsync(key);
        var second = await PostWithIdempotencyKeyAsync(key);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstWork = (await first.Content.ReadFromJsonAsync<WorkResource>())!;
        var secondWork = (await second.Content.ReadFromJsonAsync<WorkResource>())!;
        secondWork.Id.ShouldBe(firstWork.Id);
        (await CountTopLevelWorksAsync()).ShouldBe(1);
    }

    [Test]
    public async Task PostWork_SameIdempotencyKeyAtTheSameTime_CreatesOneWork()
    {
        AuthorizeAs(Roles.User);
        var key = Guid.NewGuid().ToString("D");

        var responses = await Task.WhenAll(PostWithIdempotencyKeyAsync(key), PostWithIdempotencyKeyAsync(key));

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        var ids = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<WorkResource>())!.Id));
        ids.Distinct().Count().ShouldBe(1);
        (await CountTopLevelWorksAsync()).ShouldBe(1);
    }

    [Test]
    public async Task PostWork_TwoIdempotencyKeys_CreatesTwoWorks()
    {
        AuthorizeAs(Roles.User);

        (await PostWithIdempotencyKeyAsync(Guid.NewGuid().ToString("D"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostWithIdempotencyKeyAsync(Guid.NewGuid().ToString("D"))).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await CountTopLevelWorksAsync()).ShouldBe(2);
    }

    [Test]
    public async Task PostWork_MalformedIdempotencyKey_Returns400_AndCreatesNothing()
    {
        AuthorizeAs(Roles.User);

        var response = await PostWithIdempotencyKeyAsync("not-a-guid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CountTopLevelWorksAsync()).ShouldBe(0);
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

    // ── Period-hours recalculation ──────────────────────────────────────────

    [Test]
    public async Task RecalculatePeriodHours_AsPlanner_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/RecalculatePeriodHours", new RecalculatePeriodHoursCommand(RecalculationStart, RecalculationEnd));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RecalculatePeriodHoursThorough_AsPlanner_Returns202()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/RecalculatePeriodHoursThorough", new RecalculatePeriodHoursThoroughCommand(RecalculationStart, RecalculationEnd));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [TestCase(Roles.User, "RecalculatePeriodHours")]
    [TestCase(Roles.User, "RecalculatePeriodHoursThorough")]
    [TestCase(Roles.Authorised, "RecalculatePeriodHours")]
    [TestCase(Roles.Authorised, "RecalculatePeriodHoursThorough")]
    public async Task RecalculateFrozenPeriods_AsNonAdmin_Returns403(string role, string route)
    {
        AuthorizeAs(role);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{route}", new { startDate = RecalculationStart, endDate = RecalculationEnd, confirmFrozen = true, reason = "correction" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestCase("RecalculatePeriodHours")]
    [TestCase("RecalculatePeriodHoursThorough")]
    public async Task Recalculate_AsPlanner_OverThePlannerPeriodCap_Returns400(string route)
    {
        AuthorizeAs(Roles.User);
        var end = RecalculationStart.AddMonths(PayPeriodLimits.MaxPeriodsForPlannerRecalculation + 1).AddDays(-1);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{route}", new { startDate = RecalculationStart, endDate = end });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RecalculateFrozenPeriods_AsAdmin_Returns200()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/RecalculatePeriodHours",
            new RecalculatePeriodHoursCommand(RecalculationStart, RecalculationEnd, ConfirmFrozen: true, Reason: "correction"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
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

    private async Task<HttpResponseMessage> PostWithIdempotencyKeyAsync(string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseRoute)
        {
            Content = JsonContent.Create(MinimalWork()),
        };
        request.Headers.Add(HttpHeaderNames.IdempotencyKey, key);
        return await Client.SendAsync(request);
    }

    private Task<int> CountTopLevelWorksAsync() =>
        DbContext.Work.CountAsync(w => w.ClientId == _clientId && w.ParentWorkId == null && !w.IsDeleted);

    private async Task<WorkResource> CreateWorkAsync()
    {
        var payload = MinimalWork();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkResource>())!;
    }
}
