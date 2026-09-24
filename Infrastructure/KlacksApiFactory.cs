// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * In-process test server factory for HTTP-level API tests.
 * Replaces the JWT signing key with a test secret so tokens generated
 * in tests are accepted by the running pipeline without touching the real key store, and swaps the
 * Nominatim geocoding for an offline fake so address endpoints do not depend on the network.
 * The host boots as "Development", whose appsettings turn the Slack owner bridge on, so it also switches
 * off everything the host would otherwise start on its own (same principle as
 * Klacks.IntegrationTest/HardenedTestWebApplicationFactory): every bool flag of BackgroundServiceOptions
 * plus the messaging plugin's inbound polling, the ONNX warm-up and the knowledge index sync, which no
 * API test reads. The switches go through UseSetting because Program.cs reads BackgroundServiceOptions
 * while it runs its top-level code, before ConfigureAppConfiguration callbacks apply. Migrations and the
 * seed set run in that top-level code too, so a fresh database is still brought up completely.
 */

using System.Reflection;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    /// <summary>
    /// Read by Klacks.Plugin.Messaging's registrar directly from configuration; it is not a property of
    /// BackgroundServiceOptions, so the reflection over that class does not reach it.
    /// </summary>
    public const string InboundMessagePollingSwitch = "InboundMessagePolling";

    private const string TestEnvironment = "Development";
    private const string ConnectionStringKey = "ConnectionStrings:DefaultConnection";

    /// <summary>
    /// Every configuration key this host forces to false: each bool flag of BackgroundServiceOptions and
    /// the messaging plugin's inbound polling switch.
    /// </summary>
    public static IReadOnlyList<string> DisabledBackgroundServiceSwitches { get; } =
        typeof(BackgroundServiceOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(bool))
            .Select(property => property.Name)
            .Append(InboundMessagePollingSwitch)
            .Select(name => ConfigurationPath.Combine(BackgroundServiceOptions.SectionName, name))
            .ToList();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);
        builder.UseSetting(ConnectionStringKey, TestConnectionString);
        foreach (var key in DisabledBackgroundServiceSwitches)
        {
            builder.UseSetting(key, bool.FalseString);
        }

        builder.UseSetting(KnowledgeIndexConstants.WarmupEnabledConfigKey, bool.FalseString);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IGeocodingService, FakeGeocodingService>();
            services.RemoveAll<IKnowledgeIndexSynchronizer>();
            services.AddScoped<IKnowledgeIndexSynchronizer, NoOpKnowledgeIndexSynchronizer>();

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
