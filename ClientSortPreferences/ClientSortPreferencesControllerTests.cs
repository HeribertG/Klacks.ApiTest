// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ClientSortPreferencesController (api/backend/ClientSortPreferences).
 * Covers auth enforcement (401 without token) and GET/PUT behaviour for authenticated users.
 */

using Klacks.Api.Application.DTOs;

namespace Klacks.ApiTest.ClientSortPreferences;

[TestFixture]
public class ClientSortPreferencesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ClientSortPreferences";

    [TearDown]
    public new void BaseTearDown() { base.BaseTearDown(); }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSortOrder_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SaveSortOrder_WithoutToken_Returns401()
    {
        var payload = new List<ClientSortOrderDto>();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSortOrder_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task SaveSortOrder_WithUserRole_ReturnsNoContent()
    {
        AuthorizeAs(Roles.User);
        var payload = new List<ClientSortOrderDto>();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
