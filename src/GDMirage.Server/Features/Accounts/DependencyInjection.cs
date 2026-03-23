using System.Text;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Accounts.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GDMirage.Server.Features.Accounts;

public static class DependencyInjection
{
    public static void AddAccountServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICharacterService, CharacterService>();

        services.AddSingleton<ICharacterTokenService, CharacterTokenService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration
                .GetSection("Jwt"))
            .ValidateDataAnnotations();

        var secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is not set.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }
}
