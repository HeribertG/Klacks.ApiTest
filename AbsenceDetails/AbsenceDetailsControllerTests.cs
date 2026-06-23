// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AbsenceDetailsController (api/backend/AbsenceDetails).
 * Covers list, get, post, delete — including auth enforcement (401/403/404).
 * AbsenceDetailsController inherits InputBaseController, restricting mutations to Admin/Authorised.
 * A parent Absence is created once per fixture; AbsenceDetails are cleaned up by AbsenceId.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;

namespace Klacks.ApiTest.AbsenceDetails;

[TestFixture]
public class AbsenceDetailsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/AbsenceDetails";
    private const string AbsencesRoute = "/api/backend/Absences";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _absenceId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var absencePayload = new AbsenceResource
        {
            Name = new MultiLanguage { De = $"{TestPrefix}Absence", En = $"{TestPrefix}Absence" },
            Abbreviation = new MultiLanguage { De = "TAD", En = "TAD" },
            Description = new MultiLanguage { De = "Test absence for details", En = "Test absence for details" },
            Color = $"{TestPrefix}_FF0000",
            DefaultLength = 1,
            DefaultValue = 8,
        };

        var response = await Client.PostAsJsonAsync(AbsencesRoute, absencePayload);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<AbsenceResource>())!;
        _absenceId = created.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        AuthorizeAs(Roles.Admin);

        if (_absenceId != Guid.Empty)
            await Client.DeleteAsync($"{AbsencesRoute}/{_absenceId}");
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.AbsenceDetail.Where(a => a.AbsenceId == _absenceId && !a.IsDeleted);
        DbContext.AbsenceDetail.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAbsenceDetails_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAbsenceDetail_WithoutToken_Returns401()
    {
        var payload = MinimalAbsenceDetail();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAbsenceDetail_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalAbsenceDetail();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteAbsenceDetail_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteAbsenceDetail_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAbsenceDetails_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AbsenceDetailResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAbsenceDetail_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAbsenceDetail_WithAdminRole_ReturnsCreatedAbsenceDetail()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalAbsenceDetail();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<AbsenceDetailResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.AbsenceId.ShouldBe(_absenceId);
    }

    [Test]
    public async Task PostAbsenceDetail_WithAuthorisedRole_ReturnsCreatedAbsenceDetail()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalAbsenceDetail();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAbsenceDetail_WithAdminRole_ReturnsDeletedAbsenceDetail()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateAbsenceDetailAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<AbsenceDetailResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteAbsenceDetail_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private AbsenceDetailResource MinimalAbsenceDetail() => new()
    {
        AbsenceId = _absenceId,
        Mode = AbsenceDetailMode.TimeRange,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(16, 0),
        Duration = 8,
        DetailName = new MultiLanguage { De = "Test Detail", En = "Test Detail" },
    };

    private async Task<AbsenceDetailResource> CreateAbsenceDetailAsync()
    {
        var payload = MinimalAbsenceDetail();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AbsenceDetailResource>())!;
    }
}
