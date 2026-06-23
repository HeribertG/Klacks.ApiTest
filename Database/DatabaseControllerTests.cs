// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for DatabaseController (api/internal/Database).
 * Admin-only controller — tests verify 401 and 403 enforcement only.
 * The destructive initialize/seed endpoints are never invoked with valid credentials
 * to avoid side effects on the shared test database.
 */

namespace Klacks.ApiTest.Database;

[TestFixture]
public class DatabaseControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/internal/Database";

    // ── initialize ───────────────────────────────────────────────────────────

    [Test]
    public async Task Initialize_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/initialize", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Initialize_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/initialize", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── seed ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Seed_WithoutToken_Returns401()
    {
        var response = await Client.PostAsync($"{BaseRoute}/seed", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Seed_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsync($"{BaseRoute}/seed", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
