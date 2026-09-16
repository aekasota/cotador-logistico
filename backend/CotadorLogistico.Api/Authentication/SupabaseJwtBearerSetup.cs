using System.Text;
using CotadorLogistico.Infrastructure.Auth;
using CotadorLogistico.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CotadorLogistico.Api.Authentication;

public static class SupabaseJwtBearerSetup
{
    public const string AuthenticatedRoleClaim = "role";

    public static IServiceCollection AddSupabaseJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger(nameof(SupabaseJwtBearerSetup));

                        logger.LogWarning(context.Exception, "Falha ao validar JWT: {Reason}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<SupabaseOptions>, SupabaseJwksProvider>((options, supabaseOptions, jwksProvider) =>
            {
                var supabase = supabaseOptions.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = supabase.AuthBaseUrl,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                if (!string.IsNullOrWhiteSpace(supabase.LegacyJwtSecret))
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabase.LegacyJwtSecret));
                }
                else
                {
                    options.TokenValidationParameters.IssuerSigningKeyResolver =
                        (_, _, kid, _) => jwksProvider.ResolveSigningKeys(kid);
                }
            });

        return services;
    }
}
