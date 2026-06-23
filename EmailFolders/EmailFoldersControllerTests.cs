// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for EmailFoldersController (api/backend/ReceivedEmail/Folders).
 * Covers list, create, and delete — including auth enforcement (401).
 */

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;

namespace Klacks.ApiTest.EmailFolders;

[TestFixture]
public class EmailFoldersControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ReceivedEmail/Folders";
    private readonly string TestPrefix = $"TEST_ApiTest_{Guid.NewGuid():N}_";

    [TearDown]
    public new async Task BaseTearDown()
    {
        var stale = DbContext.EmailFolders
            .Where(f => f.Name.StartsWith(TestPrefix) && !f.IsDeleted);
        DbContext.EmailFolders.RemoveRange(stale);
        await DbContext.SaveChangesAsync();
        base.BaseTearDown();
    }

    [Test]
    public async Task GetFolders_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFolders_WithUserRole_ReturnsOk()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.GetAsync(BaseRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task PostFolder_WithoutToken_Returns401()
    {
        var payload = new CreateEmailFolderCommand($"{TestPrefix}Unauth", "INBOX.Unauth");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostFolder_WithUserRole_IsNotUnauthorized()
    {
        AuthorizeAs(Roles.User);
        var payload = new CreateEmailFolderCommand($"{TestPrefix}Post", "INBOX.Post");

        var response = await Client.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteFolder_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteFolder_UnknownId_ReturnsOkOrNotFound()
    {
        AuthorizeAs(Roles.User);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
