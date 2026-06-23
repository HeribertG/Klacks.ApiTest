// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ClientsController (api/backend/Clients).
 * Covers count, create, get, and delete — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Clients;

[TestFixture]
public class ClientsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Clients";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Client
            .Where(c => c.Name.StartsWith(TestPrefix) && !c.IsDeleted);
        DbContext.Client.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostClient_WithoutToken_Returns401()
    {
        var payload = MinimalClient($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostClient_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalClient($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET count ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetCount_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/Count");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.ShouldBeGreaterThanOrEqualTo(0);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetClient_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostClient_WithAdminRole_ReturnsCreatedClient()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalClient($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ClientResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task PostClient_WithAuthorisedRole_ReturnsCreatedClient()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalClient($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteClient_WithAdminRole_ReturnsDeletedClient()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateClientAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<ClientResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteClient_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ClientResource MinimalClient(string name) => new()
    {
        Name = name,
        FirstName = "Test",
        Gender = GenderEnum.Male,
        LegalEntity = false,
        SkipAddressValidation = true,
    };

    private async Task<ClientResource> CreateClientAsync(string name)
    {
        var payload = MinimalClient(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientResource>())!;
    }
}
