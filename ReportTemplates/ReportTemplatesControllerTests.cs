// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ReportTemplatesController (api/backend/ReportTemplates).
 * Covers auth enforcement (401/403), CRUD happy paths (GetAll, GetById, GetByType,
 * Create, Update, Delete), and 404 for unknown ids. GET endpoints are open to all
 * authenticated users; mutations (Create/Update/Delete) require the Admin role.
 */

using Klacks.Api.Application.DTOs.Reports;

namespace Klacks.ApiTest.ReportTemplates;

[TestFixture]
public class ReportTemplatesControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ReportTemplates";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAll_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Create_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalTemplate();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── GET All ──────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync(BaseRoute);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<ReportTemplateResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
    }

    // ── GET by-type ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByType_WithAdminRole_ReturnsOk()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/by-type/0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── GET single ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetById_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithAdminRole_ReturnsCreatedTemplate()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalTemplate();

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<ReportTemplateResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe(payload.Name);

        await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
    }

    // ── PUT ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_IdMismatch_Returns400()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalTemplate();
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var id = Guid.NewGuid();
        var payload = MinimalTemplate();
        payload.Id = id;

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{id}", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_UnknownId_ReturnsNoContent()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Delete_ExistingTemplate_ReturnsNoContent()
    {
        AuthorizeAs(Roles.Admin);
        var created = await CreateTemplateAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ReportTemplateResource MinimalTemplate() => new()
    {
        Name = $"{TestPrefix}Template",
        Description = "API test template",
        Type = 0,
        SourceId = "schedule",
        DataSetIds = ["work"],
    };

    private async Task<ReportTemplateResource> CreateTemplateAsync()
    {
        var payload = MinimalTemplate();
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReportTemplateResource>())!;
    }
}
