// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for ExpensesController (api/backend/Expenses).
 * Covers auth enforcement (401) and the planner floor (role User may POST, PUT and DELETE since the owner
 * decision of 2026-10-12), GET list, GET single. Expenses require a Work FK; happy-path POST is omitted
 * to avoid complex setup, so the User POST only proves the role gate is gone.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.Expenses;

[TestFixture]
public class ExpensesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Expenses";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostExpenses_WithoutToken_Returns401()
    {
        var payload = new ExpensesResource
        {
            WorkId = Guid.NewGuid(),
            Amount = 10m,
            Description = "Test",
            Taxable = true,
        };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostExpenses_WithUserRole_IsNotForbidden()
    {
        AuthorizeAs(Roles.User);
        var payload = new ExpensesResource
        {
            WorkId = Guid.NewGuid(),
            Amount = 10m,
            Description = "Test",
            Taxable = true,
        };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteExpenses_WithUserRole_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetExpenses_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ExpensesResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetExpenses_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
