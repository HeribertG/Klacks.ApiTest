// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ScheduleCommandsController (api/backend/ScheduleCommands).
 * Covers auth enforcement (401/403), GET list, GET single, POST, PUT, DELETE.
 * A Client is created once per fixture as FK dependency for ScheduleCommands.
 */

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.ScheduleCommands;

[TestFixture]
public class ScheduleCommandsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ScheduleCommands";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    private Guid _clientId;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
        AuthorizeAs(Roles.Admin);

        var clientPayload = new ClientResource
        {
            Name = $"{TestPrefix}Client",
            FirstName = "SCM",
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
        var stale = DbContext.ScheduleCommands.Where(c => c.ClientId == _clientId && !c.IsDeleted);
        DbContext.ScheduleCommands.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostScheduleCommand_WithoutToken_Returns401()
    {
        var payload = MinimalScheduleCommand();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostScheduleCommand_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalScheduleCommand();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetScheduleCommands_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ScheduleCommandResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetScheduleCommand_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostScheduleCommand_WithAdminRole_ReturnsCreatedCommand()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalScheduleCommand();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ScheduleCommandResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(_clientId);
        created.CommandKeyword.ShouldBe("FREE");
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutScheduleCommand_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalScheduleCommand();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutScheduleCommand_WithAdminRole_ReturnsUpdatedCommand()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateScheduleCommandAsync();
        created.CommandKeyword = "EARLY";

        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<ScheduleCommandResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.CommandKeyword.ShouldBe("EARLY");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteScheduleCommand_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteScheduleCommand_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteScheduleCommand_WithAdminRole_ReturnsDeletedCommand()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateScheduleCommandAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<ScheduleCommandResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private ScheduleCommandResource MinimalScheduleCommand() => new()
    {
        ClientId = _clientId,
        CurrentDate = new DateOnly(2026, 6, 20),
        CommandKeyword = "FREE",
    };

    private async Task<ScheduleCommandResource> CreateScheduleCommandAsync()
    {
        var payload = MinimalScheduleCommand();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScheduleCommandResource>())!;
    }
}
