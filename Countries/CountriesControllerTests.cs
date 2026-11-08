// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for CountriesController (api/backend/Countries).
 * Covers authorization enforcement (401 without token), successful list retrieval and the Admin-only writes:
 * countries are settings (owner decision 2026-11-08, all settings are Admin work).
 */

using Klacks.Api.Domain.DTOs.Settings;

namespace Klacks.ApiTest.Countries;

[TestFixture]
public class CountriesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Countries";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetList_WithAuthorisedRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST / PUT ──────────────────────────────────────────────────────────

    [Test]
    public async Task PostCountry_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsJsonAsync(BaseRoute, new CountryResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutCountry_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PutAsJsonAsync(BaseRoute, new CountryResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSingle_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
