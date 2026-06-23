// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * HTTP-level tests for ScheduleReportController (api/backend/ScheduleReport).
 * Covers auth enforcement (401) and input validation (400 when no file provided).
 * The send endpoint is multipart/form-data and requires an authenticated user.
 */

namespace Klacks.ApiTest.ScheduleReport;

[TestFixture]
public class ScheduleReportControllerTests : ApiTestBase
{
    private const string BaseRoute = "/api/backend/ScheduleReport";

    // ── Auth ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Send_WithoutToken_Returns401()
    {
        using var content = BuildMinimalFormContent();

        var response = await Client.PostAsync($"{BaseRoute}/send", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Input validation ─────────────────────────────────────────────────────

    [Test]
    public async Task Send_WithoutPdfFile_Returns400()
    {
        AuthorizeAs(Roles.User);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Guid.NewGuid().ToString()), "clientId");
        content.Add(new StringContent("Test Client"), "clientName");
        content.Add(new StringContent("2026-01-01"), "startDate");
        content.Add(new StringContent("2026-01-31"), "endDate");

        var response = await Client.PostAsync($"{BaseRoute}/send", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Send_WithEmptyPdfFile_Returns400()
    {
        AuthorizeAs(Roles.User);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Guid.NewGuid().ToString()), "clientId");
        content.Add(new StringContent("Test Client"), "clientName");
        content.Add(new StringContent("2026-01-01"), "startDate");
        content.Add(new StringContent("2026-01-31"), "endDate");
        var emptyFile = new ByteArrayContent(Array.Empty<byte>());
        emptyFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(emptyFile, "pdfFile", "schedule.pdf");

        var response = await Client.PostAsync($"{BaseRoute}/send", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildMinimalFormContent()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(Guid.NewGuid().ToString()), "clientId");
        content.Add(new StringContent("Test Client"), "clientName");
        content.Add(new StringContent("2026-01-01"), "startDate");
        content.Add(new StringContent("2026-01-31"), "endDate");
        var pdfBytes = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        pdfBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(pdfBytes, "pdfFile", "schedule.pdf");
        return content;
    }
}
