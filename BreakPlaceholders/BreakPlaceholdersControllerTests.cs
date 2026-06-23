// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for BreakPlaceholdersController (api/backend/BreakPlaceholders).
 * Covers auth enforcement (401), GET list via GetClientList, POST, PUT, DELETE.
 * A Client and Absence are created once per fixture as FK dependencies.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.ApiTest.BreakPlaceholders;

[TestFixture]
public class BreakPlaceholdersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/BreakPlaceholders";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _clientId;
    private Guid _absenceId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var clientPayload = new ClientResource
        {
            Name = $"{TestPrefix}Client",
            FirstName = "BPH",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var clientResponse = await Client.PostAsJsonAsync("/api/backend/Clients", clientPayload);
        clientResponse.EnsureSuccessStatusCode();
        _clientId = (await clientResponse.Content.ReadFromJsonAsync<ClientResource>())!.Id;

        var absencePayload = new AbsenceResource
        {
            Name = new MultiLanguage { De = $"{TestPrefix}Absence", En = $"{TestPrefix}Absence" },
            Abbreviation = new MultiLanguage { De = "BPH", En = "BPH" },
            Description = new MultiLanguage { De = "Break placeholder absence", En = "Break placeholder absence" },
            Color = "#FF0000",
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
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.BreakPlaceholder.Where(b => b.ClientId == _clientId && !b.IsDeleted);
        DbContext.BreakPlaceholder.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostBreakPlaceholder_WithoutToken_Returns401()
    {
        var payload = MinimalBreakPlaceholder();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetClientList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);
        var filter = new BreakFilter
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/GetClientList", filter);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ClientBreakPlaceholderResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetBreakPlaceholder_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostBreakPlaceholder_WithAdminRole_WithoutMembership_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalBreakPlaceholder();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PostBreakPlaceholder_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalBreakPlaceholder();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutBreakPlaceholder_UnknownId_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalBreakPlaceholder();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteBreakPlaceholder_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private BreakPlaceholderResource MinimalBreakPlaceholder() => new()
    {
        ClientId = _clientId,
        AbsenceId = _absenceId,
        From = new DateTime(2026, 6, 20, 8, 0, 0),
        Until = new DateTime(2026, 6, 20, 8, 30, 0),
    };

    private async Task<BreakPlaceholderResource> CreateBreakPlaceholderAsync()
    {
        var payload = MinimalBreakPlaceholder();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BreakPlaceholderResource>())!;
    }
}
