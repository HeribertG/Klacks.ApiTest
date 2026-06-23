// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for AnnotationsController (api/backend/Annotations).
 * Covers annotation listing, get, create, update, delete, and auth enforcement (401/403/404).
 */

using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.ApiTest.Annotations;

[TestFixture]
public class AnnotationsControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/Annotations";
    private const string ClientsRoute = "/api/backend/Clients";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var staleClients = DbContext.Client
            .Where(c => c.Name.StartsWith(TestPrefix) && !c.IsDeleted);
        DbContext.Client.RemoveRange(staleClients);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAnnotation_WithoutToken_Returns401()
    {
        var payload = MinimalAnnotation(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostAnnotation_WithUserRole_Returns403()
    {
        AuthorizeAs(Roles.User);
        var payload = MinimalAnnotation(Guid.NewGuid());

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteAnnotation_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAnnotations_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET list ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAnnotations_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AnnotationResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
    }

    // ── GET simple list by client ────────────────────────────────────────────

    [Test]
    public async Task GetSimpleAnnotation_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleAnnotation/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSimpleAnnotation_UnknownClientId_ReturnsEmptyList()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleAnnotation/{Guid.NewGuid()}");
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AnnotationResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
        list!.ShouldBeEmpty();
    }

    [Test]
    public async Task GetSimpleAnnotation_WithExistingAnnotation_ReturnsAnnotation()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}GetSimpleAnnot");
        await CreateAnnotationAsync(clientId);

        var response = await Client.GetAsync($"{BaseRoute}/GetSimpleAnnotation/{clientId}");
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AnnotationResource>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.ShouldNotBeNull();
        list!.ShouldNotBeEmpty();
    }

    // ── GET single ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAnnotation_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostAnnotation_WithAdminRole_ReturnsCreatedAnnotation()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostAnnot");
        var payload = MinimalAnnotation(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        var created = await response.Content.ReadFromJsonAsync<AnnotationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.ShouldNotBeNull();
        created!.Id.ShouldNotBe(Guid.Empty);
        created.ClientId.ShouldBe(clientId);
        created.Note.ShouldBe(payload.Note);
    }

    [Test]
    public async Task PostAnnotation_WithAuthorisedRole_ReturnsCreatedAnnotation()
    {
        AuthorizeAs(Roles.Authorised);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PostAnnotAuth");
        var payload = MinimalAnnotation(clientId);

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── PUT ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutAnnotation_UpdatesNote()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}PutAnnot");
        var created = await CreateAnnotationAsync(clientId);

        created.Note = "Updated note text";
        var response = await Client.PutAsJsonAsync(BaseRoute, created);
        var updated = await response.Content.ReadFromJsonAsync<AnnotationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        updated.ShouldNotBeNull();
        updated!.Note.ShouldBe("Updated note text");
    }

    [Test]
    public async Task PutAnnotation_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);
        var payload = MinimalAnnotation(Guid.NewGuid());
        payload.Id = Guid.NewGuid();

        var response = await Client.PutAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAnnotation_WithAdminRole_ReturnsDeletedAnnotation()
    {
        AuthorizeAs(Roles.Admin);
        var clientId = await CreateClientAndGetIdAsync($"{TestPrefix}DelAnnot");
        var created = await CreateAnnotationAsync(clientId);

        var response = await Client.DeleteAsync($"{BaseRoute}/{created.Id}");
        var deleted = await response.Content.ReadFromJsonAsync<AnnotationResource>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        deleted!.Id.ShouldBe(created.Id);
    }

    [Test]
    public async Task DeleteAnnotation_UnknownId_Returns404()
    {
        AuthorizeAs(Roles.Admin);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static AnnotationResource MinimalAnnotation(Guid clientId) => new()
    {
        ClientId = clientId,
        Note = "Test annotation note",
    };

    private async Task<Guid> CreateClientAndGetIdAsync(string name)
    {
        var payload = new ClientResource
        {
            Name = name,
            FirstName = "Test",
            Gender = GenderEnum.Male,
            LegalEntity = false,
            SkipAddressValidation = true,
        };
        var response = await Client.PostAsJsonAsync(ClientsRoute, payload);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<ClientResource>())!;
        return created.Id;
    }

    private async Task<AnnotationResource> CreateAnnotationAsync(Guid clientId)
    {
        var payload = MinimalAnnotation(clientId);
        var response = await Client.PostAsJsonAsync(BaseRoute, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AnnotationResource>())!;
    }
}
