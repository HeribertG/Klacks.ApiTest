// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Base class for all API tests.
 * Provides a shared KlacksApiFactory, an HttpClient, a DataBaseContext,
 * and helpers for generating signed JWT tokens per role.
 */

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Klacks.ApiTest.Infrastructure;

public abstract class ApiTestBase
{
    protected KlacksApiFactory Factory = null!;
    protected HttpClient Client = null!;
    protected DataBaseContext DbContext = null!;

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        Factory = new KlacksApiFactory();
        Client = Factory.CreateClient();
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    [SetUp]
    public void BaseSetUp()
    {
        var options = new DbContextOptionsBuilder<DataBaseContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        DbContext = new DataBaseContext(options, httpContextAccessor);
    }

    [TearDown]
    public void BaseTearDown()
    {
        DbContext?.Dispose();
        Client.DefaultRequestHeaders.Remove("Authorization");
    }

    protected void AuthorizeAs(string role)
    {
        var token = GenerateToken(Guid.NewGuid().ToString(), role);
        Client.DefaultRequestHeaders.Remove("Authorization");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    private static string GenerateToken(string userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, $"test_{userId}@test.com"),
            new(ClaimTypes.Name, $"TestUser_{userId}"),
            new(ClaimTypes.GivenName, "Test"),
            new(ClaimTypes.Surname, "User"),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KlacksApiFactory.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: KlacksApiFactory.JwtIssuer,
            audience: KlacksApiFactory.JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
