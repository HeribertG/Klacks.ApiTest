// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ScheduleNotesController (api/backend/ScheduleNotes).
 * Covers auth enforcement (401/403), GET list, GET single, POST, PUT, DELETE.
 * A Client is created once per fixture as FK dependency for ScheduleNotes.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.ScheduleNotes;

[TestFixture]
public class ScheduleNotesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ScheduleNotes";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _clientId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var clientPayload = new ClientResource
        {
            Name = $"{TestPrefix}Client",
            FirstName = "SNT",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var clientResponse = await Client.PostAsJsonAsync("/api/backend/Clients", clientPayload);
        clientResponse.EnsureSuccessStatusCode();
        _clientId = (await clientResponse.Content.ReadFromJsonAsync<ClientResource>())!.Id;
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        AuthorizeAs(Roles.Admin);

        if (_clientId != Guid.Empty)
            await Client.DeleteAsync($"/api/backend/Clients/{_clientId}");
    }

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.ScheduleNotes.Where(n => n.ClientId == _clientId && !n.IsDeleted);
        DbContext.ScheduleNotes.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostScheduleNote_WithoutToken_Returns401()
    {
        var payload = MinimalScheduleNote();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostScheduleNote_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalScheduleNote();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetScheduleNotes_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ScheduleNoteResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetScheduleNote_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostScheduleNote_WithAdminRole_ReturnsCreatedNote()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalScheduleNote();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ScheduleNoteResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(_clientId);
        created.Content.ShouldBe("Test note");
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutScheduleNote_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalScheduleNote();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutScheduleNote_WithAdminRole_ReturnsUpdatedNote()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateScheduleNoteAsync();
        created.Content = "Updated content";

        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<ScheduleNoteResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Content.ShouldBe("Updated content");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteScheduleNote_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteScheduleNote_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteScheduleNote_WithAdminRole_ReturnsDeletedNote()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateScheduleNoteAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<ScheduleNoteResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private ScheduleNoteResource MinimalScheduleNote() => new()
    {
        ClientId = _clientId,
        CurrentDate = new DateOnly(2026, 6, 20),
        Content = "Test note",
    };

    private async Task<ScheduleNoteResource> CreateScheduleNoteAsync()
    {
        var payload = MinimalScheduleNote();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScheduleNoteResource>())!;
    }
}
