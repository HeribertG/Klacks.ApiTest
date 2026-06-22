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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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
