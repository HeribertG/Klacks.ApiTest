// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AbsencesController (api/backend/Absences).
 * Covers list, get, post, delete — including auth enforcement (401/403/404).
 * AbsencesController inherits InputBaseController which restricts mutations to Admin/Authorised.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;

namespace Klacks.ApiTest.Absences;

[TestFixture]
public class AbsencesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Absences";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Absence
            .Where(a => !a.IsDeleted && a.Color.StartsWith(TestPrefix));
        DbContext.Absence.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAbsence_WithoutToken_Returns401()
    {
        var payload = MinimalAbsence($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAbsence_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalAbsence($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteAbsence_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAbsences_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AbsenceResource>>();
        list.ShouldNotBeNull();
    }

    [Test]
    public async Task GetVisibleAbsences_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/visible");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAbsence_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAbsence_WithAdminRole_ReturnsCreatedAbsence()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalAbsence($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<AbsenceResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task PostAbsence_WithAuthorisedRole_ReturnsCreatedAbsence()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalAbsence($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAbsence_WithAdminRole_ReturnsDeletedAbsence()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateAbsenceAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<AbsenceResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteAbsence_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static AbsenceResource MinimalAbsence(string tag) => new()
    {
        Name = new MultiLanguage { De = tag, En = tag },
        Abbreviation = new MultiLanguage { De = "TST", En = "TST" },
        Description = new MultiLanguage { De = "Test absence", En = "Test absence" },
        Color = $"{tag}_FF0000",
        DefaultLength = 1,
        DefaultValue = 8,
    };

    private async Task<AbsenceResource> CreateAbsenceAsync(string tag)
    {
        var payload = MinimalAbsence(tag);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AbsenceResource>())!;
    }
}
