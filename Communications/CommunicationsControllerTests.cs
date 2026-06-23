// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for CommunicationsController (api/backend/Communications).
 * Covers communication listing, types, create, update, delete, and auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Communications;

[TestFixture]
public class CommunicationsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Communications";
    private const string ClientsRoute = "/api/backend/Clients";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var staleClients = DbContext.Client
            .Where(c => c.Name.StartsWith(TestPrefix) && !c.IsDeleted);
        DbContext.Client.RemoveRange(staleClients);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostCommunication_WithoutToken_Returns401()
    {
        var payload = MinimalCommunication(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostCommunication_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalCommunication(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteCommunication_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetCommunication_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCommunication_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<CommunicationResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
    }

    // ── GET communication types ──────────────────────────────────────────────

    [Test]
    public async Task GetCommunicationTypes_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/CommunicationTypes");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetCommunicationTypes_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/CommunicationTypes");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetCommunication_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostCommunication_WithAdminRole_ReturnsCreatedCommunication()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostComm");
        var payload = MinimalCommunication(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<CommunicationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(clientId);
    }

    [Test]
    public async Task PostCommunication_WithAuthorisedRole_ReturnsCreatedCommunication()
    {
        AuthorizeAs(Roles.Authorised);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostCommAuth");
        var payload = MinimalCommunication(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutCommunication_UpdatesValue()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PutComm");
        var created = await CreateCommunicationAsync(clientId);

        created.Value = "079 999 88 77";
        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<CommunicationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Value.ShouldBe("079 999 88 77");
    }

    [Test]
    public async Task PutCommunication_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalCommunication(Guid.NewGuid());
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteCommunication_WithAdminRole_ReturnsDeletedCommunication()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}DelComm");
        var created = await CreateCommunicationAsync(clientId);

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<CommunicationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteCommunication_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static CommunicationResource MinimalCommunication(Guid clientId) => new()
    {
        ClientId = clientId,
        Type = CommunicationTypeEnum.PrivateCellPhone,
        Value = "079 123 45 67",
    };

    private async Task<Guid> CreateClientAndGetIdAsync(string name)
    {
        var payload = new ClientResource
        {
            Name = name,
            FirstName = "Test",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var response = await Client.PostAsJsonAsync(ClientsRoute, payload);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<ClientResource>())!;
        return created.Id;
    }

    private async Task<CommunicationResource> CreateCommunicationAsync(Guid clientId)
    {
        var payload = MinimalCommunication(clientId);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommunicationResource>())!;
    }
}
