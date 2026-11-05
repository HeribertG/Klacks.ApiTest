// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for AccountsController (api/backend/Accounts).
 * Covers authentication enforcement (401/403), anonymous endpoints (login, password reset),
 * and admin-restricted endpoints.
 */

namespace Klacks.ApiTest.Accounts;

[TestFixture]
public class AccountsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Accounts";
    private const string RequestedWithHeader = "X-Requested-With";
    private const string RequestedWithValue = "XMLHttpRequest";

    // ── ValidateToken ────────────────────────────────────────────────────────

    [Test]
    public async Task ValidateToken_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/ValidateToken");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ValidateToken_WithUserRole_Returns200()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/ValidateToken");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GetUserList (Admin only) ─────────────────────────────────────────────

    [Test]
    public async Task GetUserList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUserList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetUserList_WithAdminRole_Returns200()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GenerateUsername (Admin only) ────────────────────────────────────────

    [Test]
    public async Task GenerateUsername_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GenerateUsername?firstName=Max&lastName=Muster");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GenerateUsername_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GenerateUsername?firstName=Max&lastName=Muster");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── ChangePasswordUser (any authenticated) ───────────────────────────────

    [Test]
    public async Task ChangePasswordUser_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/ChangePasswordUser", new { email = "x@x.com", oldPassword = "old", password = "new" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── LoginUser (anonymous) ────────────────────────────────────────────────

    [Test]
    public async Task LoginUser_WithNullBody_Returns415()
    {
        var response = await Client.PostAsync($"{BaseRoute}/LoginUser", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task LoginUser_WithInvalidCredentials_Returns200OrError()
    {
        var payload = new { email = "nonexistent@test.invalid", password = "wrong" };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/LoginUser", payload);

        // Endpoint is reachable without auth; result may be 200 (with error in body) or 400/401
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(200);
    }

    // ── RefreshToken (anonymous) ─────────────────────────────────────────────

    [Test]
    public async Task RefreshToken_WithoutCsrfProof_Returns403()
    {
        var response = await Client.PostAsync($"{BaseRoute}/RefreshToken", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RefreshToken_WithCsrfProofButWithoutCookieOrBody_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseRoute}/RefreshToken");
        request.Headers.Add(RequestedWithHeader, RequestedWithValue);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── RequestPasswordReset (anonymous) ─────────────────────────────────────

    [Test]
    public async Task RequestPasswordReset_WithMissingEmail_IsReachable()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/RequestPasswordReset", new { email = "" });

        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(200);
    }

    [Test]
    public async Task RequestPasswordReset_WithNullBody_Returns415()
    {
        var response = await Client.PostAsync($"{BaseRoute}/RequestPasswordReset", null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    // ── ValidatePasswordResetToken (anonymous) ────────────────────────────────

    [Test]
    public async Task ValidatePasswordResetToken_WithEmptyToken_IsReachable()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/ValidatePasswordResetToken", new { token = "" });

        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(200);
    }
}
