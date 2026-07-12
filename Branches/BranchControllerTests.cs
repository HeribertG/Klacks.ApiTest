// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for BranchController (api/backend/Branch).
 * Covers full CRUD lifecycle, authorization enforcement (401/403),
 * and 404 for unknown resources.
 */

namespace Klacks.ApiTest.Branches;

[TestFixture]
public class BranchControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Branch";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Branch
            .Where(b => b.Name.StartsWith(TestPrefix) && !b.IsDeleted);
        DbContext.Branch.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetBranchList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetBranchList");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetBranchList_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetBranchList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetBranchList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetBranchList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetBranch_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetBranch/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddBranch_WithAdminRole_ReturnsCreatedBranch()
    {
        AuthorizeAs(Roles.Admin);
        var payload = new Branch { Name = $"{TestPrefix}Add", Address = "Teststrasse 1" };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddBranch", payload);
        var created = await response.Content.ReadFromJsonAsync<Branch>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Name.ShouldBe(payload.Name);
        created.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task AddBranch_WithoutToken_Returns401()
    {
        var payload = new Branch { Name = $"{TestPrefix}Unauth" };

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddBranch", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutBranch_UpdatesName()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateBranchAsync($"{TestPrefix}PutOriginal");

        created.Name = $"{TestPrefix}PutUpdated";
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/PutBranch", created);
        var updated = await response.Content.ReadFromJsonAsync<Branch>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated!.Name.ShouldBe($"{TestPrefix}PutUpdated");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteBranch_ReturnsNoContent()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateBranchAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/DeleteBranch/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteBranch_UnknownId_ReturnsNoContent()
    {
        AuthorizeAs(Roles.Admin);

        // API does not verify existence before delete — returns NoContent regardless
        var response = await Client.DeleteAsync($"{BaseRoute}/DeleteBranch/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Branch> CreateBranchAsync(string name)
    {
        var payload = new Branch { Name = name, Address = "Teststrasse 1" };
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddBranch", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Branch>())!;
    }
}
