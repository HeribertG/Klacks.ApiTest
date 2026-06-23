// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for RouteOptimizationController (api/backend/RouteOptimization).
 * Covers auth enforcement (401) for distance-matrix, optimize-route, autofill, and geocode-all.
 */

namespace Klacks.ApiTest.RouteOptimization;

[TestFixture]
public class RouteOptimizationControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/RouteOptimization";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetDistanceMatrix_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/distance-matrix?containerId={Guid.NewGuid()}&weekday=1");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostOptimizeRoute_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/optimize-route", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAutofill_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/autofill", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostGeocodeAll_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/geocode-all", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
