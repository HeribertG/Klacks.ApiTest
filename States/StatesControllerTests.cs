// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for StatesController (api/backend/States).
 * GET list is public (no auth required); GET single requires a token;
 * Delete/Post/Put require Admin or Authorised role.
 */

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.ApiTest.States;

[TestFixture]
public class StatesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/States";

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetStateList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetStateList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetState_UnknownId_WithAdminRole_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostState_WithoutToken_Returns401()
    {
        var payload = MinimalState();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostState_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalState();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostState_WithAdminRole_ReturnsCreatedState()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalState();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<StateResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Abbreviation.ShouldBe(payload.Abbreviation);

        await CleanUpStateAsync(created.Id);
    }

    [Test]
    public async Task PostState_WithAuthorisedRole_ReturnsCreatedState()
    {
        AuthorizeAs(Roles.Authorised);
        var payload = MinimalState();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<StateResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();

        AuthorizeAs(Roles.Admin);
        await CleanUpStateAsync(created!.Id);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteState_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteState_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteState_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteState_WithAdminRole_ReturnsDeletedState()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateStateAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<StateResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static StateResource MinimalState() => new()
    {
        Abbreviation = "XT",
        CountryPrefix = "XX",
        Name = new Api.Domain.Common.MultiLanguage { De = "Teststate", En = "Teststate" },
    };

    private async Task<StateResource> CreateStateAsync()
    {
        var payload = MinimalState();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StateResource>())!;
    }

    private async Task CleanUpStateAsync(Guid id)
    {
        await Client.DeleteAsync($"{BaseRoute}/{id}");
    }
}
