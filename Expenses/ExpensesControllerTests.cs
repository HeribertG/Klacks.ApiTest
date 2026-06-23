// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ExpensesController (api/backend/Expenses).
 * Covers auth enforcement (401/403), GET list, GET single.
 * Expenses require a Work FK; happy-path POST is omitted to avoid complex setup.
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
    public async Task PostExpenses_WithUserRole_Returns403()
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

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteExpenses_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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
