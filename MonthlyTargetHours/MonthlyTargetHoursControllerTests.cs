// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for MonthlyTargetHoursController (api/backend/MonthlyTargetHours).
 * The monthly target hours are settings: Post and Put are Admin-only (owner decision 2026-11-08, all settings are Admin work),
 * Delete is Admin-only from InputBaseController, the list stays readable for planners and supervisors.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.MonthlyTargetHours;

[TestFixture]
public class MonthlyTargetHoursControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/MonthlyTargetHours";

    [Test]
    public async Task GetMonthlyTargetHours_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMonthlyTargetHours_WithAuthorisedRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMonthlyTargetHours_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostMonthlyTargetHours_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsJsonAsync(BaseRoute, new MonthlyTargetHoursResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutMonthlyTargetHours_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PutAsJsonAsync(BaseRoute, new MonthlyTargetHoursResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostMonthlyTargetHours_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(BaseRoute, new MonthlyTargetHoursResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
