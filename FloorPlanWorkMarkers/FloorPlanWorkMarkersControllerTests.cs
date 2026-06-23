// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for FloorPlanWorkMarkersController (api/backend/FloorPlanWorkMarkers).
 * Covers get by floor plan, delete — including auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.FloorPlans;

namespace Klacks.ApiTest.FloorPlanWorkMarkers;

[TestFixture]
public class FloorPlanWorkMarkersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/FloorPlanWorkMarkers";

    [TearDown]
    public new void BaseTearDown()
    {
        base.BaseTearDown();
    }

    [Test]
    public async Task PostFloorPlanWorkMarker_WithoutToken_Returns401()
    {
        var payload = new FloorPlanWorkMarkerResource
        {
            FloorPlanId = Guid.NewGuid(),
        };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostFloorPlanWorkMarker_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = new FloorPlanWorkMarkerResource
        {
            FloorPlanId = Guid.NewGuid(),
        };

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteFloorPlanWorkMarker_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteFloorPlanWorkMarker_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetByFloorPlan_UnknownFloorPlanId_ReturnsOkWithEmptyArray()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ByFloorPlan/{Guid.NewGuid()}");
        var result = await response.Content.ReadFromJsonAsync<List<FloorPlanWorkMarkerResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result!.ShouldBeEmpty();
    }

    [Test]
    public async Task DeleteFloorPlanWorkMarker_UnknownId_Returns404WithAdminRole()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
