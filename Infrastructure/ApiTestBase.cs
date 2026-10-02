// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Base class for all API tests.
 * Provides a shared KlacksApiFactory, an HttpClient, a DataBaseContext,
 * and helpers for generating signed JWT tokens per role. AuthorizeAs also persists a matching AppUser
 * (and its role membership), because role checks such as the group visibility scope read the roles
 * from the database, not from the token claim.
 */

using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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

    private const string TestUserNamePrefix = "INTEGRATION_TEST_APITEST_";
    private const string TestUserEmailDomain = "@apitest.klacks.local";
    private readonly List<string> _createdUserIds = [];

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        Factory = Klacks.ApiTest.TestAssemblySetup.SharedFactory;
        Client = Factory.CreateClient();

        // The first HttpClient request across the whole assembly runs right after the shared host has
        // booted and self-seeded a fresh CI database (migrations + full seed set). On a cold, resource
        // constrained CI runner that first request can exceed HttpClient's default 100s timeout and
        // abort with a TaskCanceledException (client abort), which fails an otherwise-passing test.
        // A generous timeout removes that cold-start flake without weakening any assertion.
        Client.Timeout = TimeSpan.FromMinutes(3);
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        Client?.Dispose();
        // Factory is owned by TestAssemblySetup — not disposed here
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
        DeleteCreatedUsersAsync().GetAwaiter().GetResult();
    }

    protected void AuthorizeAs(string role)
    {
        var userId = CreateUserAsync(role).GetAwaiter().GetResult();
        var token = GenerateToken(userId, role);
        Client.DefaultRequestHeaders.Remove("Authorization");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    private async Task<string> CreateUserAsync(string role)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userId = Guid.NewGuid().ToString();
        var user = new AppUser
        {
            Id = userId,
            UserName = TestUserNamePrefix + userId,
            Email = TestUserNamePrefix + userId + TestUserEmailDomain,
            FirstName = "Test",
            LastName = "User",
        };

        var created = await userManager.CreateAsync(user);
        created.Succeeded.ShouldBeTrue(string.Join("; ", created.Errors.Select(e => e.Description)));
        _createdUserIds.Add(userId);

        if (await roleManager.RoleExistsAsync(role))
        {
            var added = await userManager.AddToRoleAsync(user, role);
            added.Succeeded.ShouldBeTrue(string.Join("; ", added.Errors.Select(e => e.Description)));
        }

        return userId;
    }

    private async Task DeleteCreatedUsersAsync()
    {
        if (_createdUserIds.Count == 0)
        {
            return;
        }

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        foreach (var userId in _createdUserIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is not null)
            {
                await userManager.DeleteAsync(user);
            }
        }

        _createdUserIds.Clear();
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
