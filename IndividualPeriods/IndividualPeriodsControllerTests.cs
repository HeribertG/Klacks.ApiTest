// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for IndividualPeriodsController (api/backend/IndividualPeriods).
 * Individual periods are settings: Post and Put are Admin-only (owner decision 2026-11-08, all settings are Admin work),
 * Delete is Admin-only from InputBaseController, the list stays readable for planners and supervisors.
 */

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.ApiTest.IndividualPeriods;

[TestFixture]
public class IndividualPeriodsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/IndividualPeriods";

    [Test]
    public async Task GetIndividualPeriods_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetIndividualPeriods_WithAuthorisedRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetIndividualPeriods_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostIndividualPeriod_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsJsonAsync(BaseRoute, new IndividualPeriodResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PutIndividualPeriod_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PutAsJsonAsync(BaseRoute, new IndividualPeriodResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostIndividualPeriod_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(BaseRoute, new IndividualPeriodResource());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
