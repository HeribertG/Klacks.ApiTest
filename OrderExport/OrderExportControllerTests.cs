// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for OrderExportController (api/backend/OrderExport).
 * Covers auth enforcement (401) and authenticated GET orders (200).
 */

namespace Klacks.ApiTest.OrderExport;

[TestFixture]
public class OrderExportControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/OrderExport";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostOrderExport_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET orders ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetOrders_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
