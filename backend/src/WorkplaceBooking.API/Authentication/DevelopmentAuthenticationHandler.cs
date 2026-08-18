using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

namespace WorkplaceBooking.Api.Authentication;

public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only use development authentication when explicitly enabled
        if (!_configuration.GetValue<bool>("Authentication:UseDevelopmentMode"))
        {
            return AuthenticateResult.NoResult();
        }

        var devUserId = _configuration["Authentication:DevelopmentUserId"] ?? "11111111-1111-1111-1111-111111111111";
        var devEmail = _configuration["Authentication:DevelopmentUserEmail"] ?? "dev@local.com";
        var devName = _configuration["Authentication:DevelopmentUserName"] ?? "Developer Local";
        var devRoles = _configuration.GetSection("Authentication:DevelopmentRoles").Get<List<string>>() ?? new[] { "GLOBAL_ADMIN" };
        var devProfiles = _configuration.GetSection("Authentication:DevelopmentBusinessProfiles").Get<List<string>>() ?? new List<string> { "GLOBAL_ADMIN" };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, devUserId),
            new Claim(ClaimTypes.Email, devEmail),
            new Claim(ClaimTypes.Name, devName),
            new Claim(JwtRegisteredClaimNames.Sub, devUserId),
            new Claim(JwtRegisteredClaimNames.Email, devEmail),
            new Claim(JwtRegisteredClaimNames.Name, devName),
            new Claim(JwtRegisteredClaimNames.GivenName, "Developer"),
            new Claim(JwtRegisteredClaimNames.FamilyName, "Local"),
        };

        // Add roles
        var roles = _configuration.GetSection("Authentication:DevelopmentRoles").Get<List<string>>() ?? new[] { "GLOBAL_ADMIN" };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add business profiles
        var profiles = _configuration.GetSection("Authentication:DevelopmentBusinessProfiles").Get<List<string>>() ?? new List<string> { "GLOBAL_ADMIN" };
        foreach (var profile in profiles)
        {
            claims.Add(new Claim("business_profile", profile));
        }

        var identity = new ClaimsIdentity(claims, "Development");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Development");

        return AuthenticateResult.Success(ticket);
    }
}