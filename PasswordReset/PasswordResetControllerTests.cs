// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for PasswordResetController (/reset-password).
 * The controller extends BaseWebController which has no [Authorize]; the endpoint is public.
 */

namespace Klacks.ApiTest.PasswordReset;

[TestFixture]
public class PasswordResetControllerTests : ApiTestBase
{
    // ── Public access ───────────────────────────────────────────────────────

    [Test]
    public async Task GetResetPassword_WithToken_IsNotUnauthorized()
    {
        var response = await Client.GetAsync("/reset-password?token=testtoken");

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }
}
