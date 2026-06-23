// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for FloorPlansController (api/backend/FloorPlans).
 * Covers create, get, delete — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.FloorPlans;

namespace Klacks.ApiTest.FloorPlans;

[TestFixture]
public class FloorPlansControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/FloorPlans";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.FloorPlan
            .Where(f => f.Name.StartsWith(TestPrefix) && !f.IsDeleted);
        DbContext.FloorPlan.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    [Test]
    public async Task PostFloorPlan_WithoutToken_Returns401()
    {
        var payload = MinimalFloorPlan($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostFloorPlan_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalFloorPlan($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteFloorPlan_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteFloorPlan_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetFloorPlan_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetFloorPlans_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostFloorPlan_WithAdminRole_ReturnsCreatedFloorPlan()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalFloorPlan($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<FloorPlanResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task DeleteFloorPlan_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static FloorPlanResource MinimalFloorPlan(string name) => new()
    {
        Name = name,
    };
}
