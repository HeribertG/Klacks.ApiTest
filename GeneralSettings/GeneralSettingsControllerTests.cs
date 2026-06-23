// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for GeneralSettingsController (api/backend/GeneralSettings).
 * All endpoints require the Admin role; covers auth enforcement (401/403) and CRUD happy paths.
 */

namespace Klacks.ApiTest.GeneralSettings;

[TestFixture]
public class GeneralSettingsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/GeneralSettings";
    private readonly string TestSettingType = $"TEST_ApiTest_{Guid.NewGuid():N}";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Settings
            .Where(s => s.Type.StartsWith("TEST_ApiTest_"));
        DbContext.Settings.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSettingsList_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetSettingsList");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSettingsList_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetSettingsList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetSettingsList_WithAuthorisedRole_Returns403()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/GetSettingsList");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetSettingsList_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetSettingsList");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<Settings>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSetting_UnknownType_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/GetSetting/NONEXISTENT_TYPE_{Guid.NewGuid():N}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST AddSetting ──────────────────────────────────────────────────────

    [Test]
    public async Task AddSetting_WithoutToken_Returns401()
    {
        var payload = MinimalSetting();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddSetting", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AddSetting_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalSetting();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddSetting", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddSetting_WithAdminRole_ReturnsCreatedSetting()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalSetting();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddSetting", payload);
        var created = await response.Content.ReadFromJsonAsync<Settings>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Type.ShouldBe(TestSettingType);
        created.Value.ShouldBe("test-value");
    }

    // ── PUT PutSetting ───────────────────────────────────────────────────────

    [Test]
    public async Task PutSetting_WithAdminRole_ReturnsUpdatedSetting()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateSettingAsync();

        created.Value = "updated-value";
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/PutSetting", created);
        var updated = await response.Content.ReadFromJsonAsync<Settings>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Value.ShouldBe("updated-value");
    }

    [Test]
    public async Task PutSetting_WithoutToken_Returns401()
    {
        var payload = MinimalSetting();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/PutSetting", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET round-trip ───────────────────────────────────────────────────────

    [Test]
    public async Task GetSetting_AfterAdd_ReturnsSameSetting()
    {
        AuthorizeAs(Roles.Admin);
        await CreateSettingAsync();

        var response = await Client.GetAsync($"{BaseRoute}/GetSetting/{TestSettingType}");
        var retrieved = await response.Content.ReadFromJsonAsync<Settings>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        retrieved.ShouldNotBeNull();
        retrieved!.Type.ShouldBe(TestSettingType);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private Settings MinimalSetting() => new()
    {
        Type = TestSettingType,
        Value = "test-value",
    };

    private async Task<Settings> CreateSettingAsync()
    {
        var payload = MinimalSetting();
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/AddSetting", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Settings>())!;
    }
}
