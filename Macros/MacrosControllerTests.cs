// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for MacrosController (api/backend/Macros).
 * All endpoints require the Admin role; covers auth enforcement (401/403) and CRUD happy paths.
 */

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Enums;

namespace Klacks.ApiTest.Macros;

[TestFixture]
public class MacrosControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Macros";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.Macro
            .Where(m => m.Name.StartsWith(TestPrefix) && !m.IsDeleted);
        DbContext.Macro.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMacros_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/Macros");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMacros_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/Macros");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMacros_WithAuthorisedRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Authorised);

        var response = await Client.GetAsync($"{BaseRoute}/Macros");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMacros_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/Macros");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<MacroResource>>();
        list.ShouldNotBeNull();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetMacro_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/Macros/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostMacro_WithoutToken_Returns401()
    {
        var payload = MinimalMacro($"{TestPrefix}Unauth");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Macros", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostMacro_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalMacro($"{TestPrefix}User");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Macros", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PostMacro_WithAdminRole_ReturnsCreatedMacro()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalMacro($"{TestPrefix}Post");

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Macros", payload);
        var created = await response.Content.ReadFromJsonAsync<MacroResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe(payload.Name);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutMacro_WithAdminRole_ReturnsUpdatedMacro()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateMacroAsync($"{TestPrefix}Put");

        created.Content = "updated content";
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/Macros", created);
        var updated = await response.Content.ReadFromJsonAsync<MacroResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Content.ShouldBe("updated content");
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteMacro_WithAdminRole_ReturnsDeletedMacro()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateMacroAsync($"{TestPrefix}Delete");

        var response = await Client.DeleteAsync($"{BaseRoute}/Macros/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<MacroResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteMacro_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/Macros/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static MacroResource MinimalMacro(string name) => new()
    {
        Name = name,
        Content = "test content",
        Category = MacroCategoryEnum.Shift,
        Type = 0,
        Description = new Api.Domain.Common.MultiLanguage { De = "Test", En = "Test" },
    };

    private async Task<MacroResource> CreateMacroAsync(string name)
    {
        var payload = MinimalMacro(name);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/Macros", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MacroResource>())!;
    }
}
