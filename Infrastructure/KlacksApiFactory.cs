// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * In-process test server factory for HTTP-level API tests.
 * Replaces the JWT signing key with a test secret so tokens generated
 * in tests are accepted by the running pipeline without touching the real key store.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Klacks.ApiTest.Infrastructure;

public class KlacksApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "tqXc2HF1RDsi/N1LMkGIVrgFSVuJ9PBmFg/QrgzqlfQ=";
    public const string JwtIssuer = "https://localhost:44371";
    public const string JwtAudience = "https://localhost:44371";

    // Program.cs appends ";Minimum Pool Size=5;Maximum Pool Size=150;" whenever "Command Timeout"
    // is absent from the connection string, and the default Npgsql idle lifetime (300s) then keeps
    // that minimum open for the rest of the test run. This host is shared for the whole assembly
    // (see TestAssemblySetup), but running alongside another test project's hosts against the same
    // port-5434 Postgres instance can still push the combined connection count past max_connections
    // (100) without a tighter cap here. Same mitigation as Klacks.IntegrationTest/TestHostDatabase.cs.
    private const string TestConnectionString =
        "User ID=postgres;Password=admin;Host=localhost;Port=5434;Database=klacks;Pooling=true;"
        + "Command Timeout=60;Timeout=30;Minimum Pool Size=0;Maximum Pool Size=40;"
        + "Connection Idle Lifetime=10;Connection Pruning Interval=5;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.ConfigureServices(services =>
        {
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });
        });
    }
}
