// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * HTTP-level tests for the stand-in endpoints of ClientsController (api/backend/Clients/StandIns and
 * api/backend/Clients/StandIns/{id}/Convert): 401 without token, 403 for a caller without a role (planner), past the
 * rights check for a supervisor, and the round trip create, then convert, for an admin.
 */

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.ApiTest.Clients;

[TestFixture]
public class ClientsStandInsControllerTests : ApiTestBase
{
    private const string CreateRoute = "/api/backend/Clients/StandIns";
    private readonly string _testPrefix = $"INTEGRATION_TEST_StandIn_ApiTest_{Guid.NewGuid():N}_";
    private readonly Guid _groupId = Guid.NewGuid();

    [SetUp]
    public async Task SeedGroup()
    {
        DbContext.Group.Add(new Group
        {
            Id = _groupId,
            Name = _testPrefix + "Group",
            Description = _testPrefix,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
        });
        await DbContext.SaveChangesAsync();
    }

    [TearDown]
    public async Task CleanUp()
    {
        DbContext.ChangeTracker.Clear();
        var clientIds = await DbContext.Client.IgnoreQueryFilters()
            .Where(c => c.Name.StartsWith(_testPrefix))
            .Select(c => c.Id)
            .ToListAsync();
        await DbContext.GroupItem.IgnoreQueryFilters()
            .Where(g => g.GroupId == _groupId || (g.ClientId != null && clientIds.Contains(g.ClientId.Value)))
            .ExecuteDeleteAsync();
        await DbContext.Membership.IgnoreQueryFilters().Where(m => clientIds.Contains(m.ClientId)).ExecuteDeleteAsync();
        await DbContext.Client.IgnoreQueryFilters().Where(c => clientIds.Contains(c.Id)).ExecuteDeleteAsync();
        await DbContext.Group.IgnoreQueryFilters().Where(g => g.Id == _groupId).ExecuteDeleteAsync();
    }

    private CreateStandInsResource CreatePayload(int count = 2) => new()
    {
        Count = count,
        NamePattern = _testPrefix + "Fair {n}",
        GroupId = _groupId,
        ValidFrom = new DateOnly(2092, 1, 1),
    };

    private static ConvertStandInResource ConvertPayload(string name) => new()
    {
        TargetType = (int)EntityTypeEnum.Employee,
        FirstName = "Anna",
        Name = name,
        Gender = GenderEnum.Female,
    };

    [Test]
    public async Task CreateStandIns_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(CreateRoute, CreatePayload());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateStandIns_WithUserRole_Returns403_AndCreatesNothing()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(CreateRoute, CreatePayload());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await DbContext.Client.CountAsync(c => c.Name.StartsWith(_testPrefix))).ShouldBe(0);
    }

    [Test]
    public async Task CreateStandIns_WithAuthorisedRole_PassesTheRightsCheck()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsJsonAsync(CreateRoute, CreatePayload(count: 0));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ConvertStandIn_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.PostAsJsonAsync(
            $"{CreateRoute}/{Guid.NewGuid()}/Convert", ConvertPayload(_testPrefix + "Muster"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ConvertStandIn_WithAuthorisedRole_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.PostAsJsonAsync(
            $"{CreateRoute}/{Guid.NewGuid()}/Convert", ConvertPayload(_testPrefix + "Muster"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Admin_CreatesStandIns_ThenConvertsOne_IntoAnEmployee()
    {
        AuthorizeAs(Roles.Admin);

        var createResponse = await Client.PostAsJsonAsync(CreateRoute, CreatePayload());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<List<CreatedStandInResource>>();
        created.ShouldNotBeNull();
        created!.Select(c => c.Name).ShouldBe([_testPrefix + "Fair 1", _testPrefix + "Fair 2"]);

        var convertResponse = await Client.PostAsJsonAsync(
            $"{CreateRoute}/{created[0].Id}/Convert", ConvertPayload(_testPrefix + "Muster"));
        convertResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var converted = await convertResponse.Content.ReadFromJsonAsync<ClientResource>();

        converted!.Id.ShouldBe(created[0].Id);
        converted.IdNumber.ShouldBe(created[0].IdNumber);
        converted.Type.ShouldBe((int)EntityTypeEnum.Employee);
        converted.FirstName.ShouldBe("Anna");
        converted.GroupItems.ShouldHaveSingleItem().GroupId.ShouldBe(_groupId);

        var again = await Client.PostAsJsonAsync(
            $"{CreateRoute}/{created[0].Id}/Convert", ConvertPayload(_testPrefix + "Muster"));
        again.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
