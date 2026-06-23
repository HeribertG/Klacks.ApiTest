// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ContractsController (api/backend/Contracts).
 * Covers list, create, get, and delete — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.ApiTest.Contracts;

[TestFixture]
public class ContractsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Contracts";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Contract
            .Where(c => c.Name.StartsWith(TestPrefix) && !c.IsDeleted);
        DbContext.Contract.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetContracts_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostContract_WithoutToken_Returns401()
    {
        var payload = MinimalContract($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostContract_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalContract($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteContract_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteContract_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetContract_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetContracts_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contracts = await response.Content.ReadFromJsonAsync<IEnumerable<ContractResource>>();
        contracts.ShouldNotBeNull();
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostContract_WithAdminRole_ReturnsCreatedContract()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalContract($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ContractResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task PostContract_WithAuthorisedRole_ReturnsCreatedContract()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalContract($"{TestPrefix}PostAuth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteContract_WithAdminRole_ReturnsDeletedContract()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateContractAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<ContractResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteContract_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ContractResource MinimalContract(string name) => new()
    {
        Name = name,
        ValidFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PaymentInterval = PaymentInterval.Monthly,
        GuaranteedHours = 0,
        MaximumHours = 40,
        MinimumHours = 0,
        FullTime = 40,
        WorkOnMonday = true,
        WorkOnTuesday = true,
        WorkOnWednesday = true,
        WorkOnThursday = true,
        WorkOnFriday = true,
    };

    private async Task<ContractResource> CreateContractAsync(string name)
    {
        var payload = MinimalContract(name);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContractResource>())!;
    }
}
