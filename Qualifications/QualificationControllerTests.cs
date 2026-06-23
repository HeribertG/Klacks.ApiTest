// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for QualificationController (api/backend/Qualification).
 * All endpoints require the Admin role; covers auth enforcement (401/403) and CRUD happy paths.
 */

using Klacks.Api.Domain.Enums;

namespace Klacks.ApiTest.Qualifications;

[TestFixture]
public class QualificationControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Qualification";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Qualification
            .Where(q => !q.IsDeleted)
            .AsEnumerable()
            .Where(q => q.Name.En != null && q.Name.En.StartsWith(TestPrefix))
            .ToList();
        DbContext.Qualification.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetQualificationList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetQualificationList");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetQualificationList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetQualificationList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetQualificationList_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/GetQualificationList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetQualificationList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetQualificationList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<Qualification>>();
        list.ShouldNotBeNull();
    }

    // ── POST AddQualification ────────────────────────────────────────────────

    [Test]
    public async Task AddQualification_WithoutToken_Returns401()
    {
        var payload = MinimalQualification($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddQualification", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AddQualification_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalQualification($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddQualification", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddQualification_WithAdminRole_ReturnsCreatedQualification()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalQualification($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddQualification", payload);
        var created = await response.Content.ReadFromJsonAsync<Qualification>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Name.En.ShouldBe(payload.Name.En);
    }

    // ── PUT PutQualification ─────────────────────────────────────────────────

    [Test]
    public async Task PutQualification_WithAdminRole_ReturnsUpdatedQualification()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateQualificationAsync($"{TestPrefix}Put");

        created.Emoji = "🔧";
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/PutQualification", created);
        var updated = await response.Content.ReadFromJsonAsync<Qualification>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Emoji.ShouldBe("🔧");
    }

    [Test]
    public async Task PutQualification_WithoutToken_Returns401()
    {
        var payload = MinimalQualification($"{TestPrefix}Unauth");

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/PutQualification", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── DELETE DeleteQualification ───────────────────────────────────────────

    [Test]
    public async Task DeleteQualification_WithAdminRole_ReturnsNoContent()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateQualificationAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/DeleteQualification/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteQualification_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/DeleteQualification/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteQualification_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/DeleteQualification/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Qualification MinimalQualification(string name) => new()
    {
        Name = new Api.Domain.Common.MultiLanguage { De = name, En = name },
        Type = QualificationType.Work,
        Category = QualificationCategory.None,
        IsTimeLimited = false,
    };

    private async Task<Qualification> CreateQualificationAsync(string name)
    {
        var payload = MinimalQualification(name);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddQualification", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Qualification>())!;
    }
}
