// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AddressesController (api/backend/Addresses).
 * Covers client address listing, get, create, update, delete, and auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Addresses;

[TestFixture]
public class AddressesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Addresses";
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
    public async Task PostAddress_WithoutToken_Returns401()
    {
        var payload = MinimalAddress(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAddress_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalAddress(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutAddress_WithoutToken_Returns401()
    {
        var payload = MinimalAddress(Guid.NewGuid());

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteAddress_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetClientAddressList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ClientAddressList/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list by client ──────────────────────────────────────────────────

    [Test]
    public async Task GetClientAddressList_UnknownClientId_ReturnsEmptyList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ClientAddressList/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AddressResource>>();
        list.ShouldNotBeNull();
        list!.ShouldBeEmpty();
    }

    [Test]
    public async Task GetClientAddressList_WithExistingAddress_ReturnsAddress()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}AddrList");
        await CreateAddressAsync(clientId);

        var response = await Client.GetAsync($"{BaseRoute}/ClientAddressList/{clientId}");
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AddressResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
        list!.ShouldNotBeEmpty();
    }

    // ── GET simple list ─────────────────────────────────────────────────────

    [Test]
    public async Task GetSimpleAddress_UnknownClientId_ReturnsEmptyList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleAddress/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AddressResource>>();
        list.ShouldNotBeNull();
        list!.ShouldBeEmpty();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAddress_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAddress_WithAdminRole_ReturnsCreatedAddress()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostAddr");
        var payload = MinimalAddress(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<AddressResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(clientId);
    }

    [Test]
    public async Task PostAddress_WithAuthorisedRole_ReturnsCreatedAddress()
    {
        AuthorizeAs(Roles.Authorised);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostAddrAuth");
        var payload = MinimalAddress(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutAddress_UpdatesAddressLine2()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PutAddr");
        var created = await CreateAddressAsync(clientId);

        created.AddressLine2 = "c/o Test";
        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<AddressResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.AddressLine2.ShouldBe("c/o Test");
    }

    [Test]
    public async Task PutAddress_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalAddress(Guid.NewGuid());
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAddress_WithAdminRole_ReturnsDeletedAddress()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}DelAddr");
        var created = await CreateAddressAsync(clientId);

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<AddressResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteAddress_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static AddressResource MinimalAddress(Guid clientId) => new()
    {
        ClientId = clientId,
        City = "Bern",
        Zip = "3011",
        Country = "CH",
        Type = AddressTypeEnum.Employee,
        ValidFrom = DateTime.Today,
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

    private async Task<AddressResource> CreateAddressAsync(Guid clientId)
    {
        var payload = MinimalAddress(clientId);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AddressResource>())!;
    }
}
