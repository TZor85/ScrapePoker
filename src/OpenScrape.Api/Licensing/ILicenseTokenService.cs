using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OpenScrape.Api.Licensing;

/// <summary>Claims mínimos que viajan en el token.</summary>
public readonly record struct LicenseClaims(Guid Subject, string Plan, DateTimeOffset ExpiresAt);

/// <summary>Emite y valida los tokens de licencia.</summary>
public interface ILicenseTokenService
{
    (string Token, DateTimeOffset ExpiresAt) Issue(License license);

    bool TryValidate(string token, out LicenseClaims claims);
}

/// <summary>JWT HS256 con vida corta; el cliente lo renueva con la misma key.</summary>
public sealed class JwtLicenseTokenService : ILicenseTokenService
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);
    private static readonly JwtSecurityTokenHandler Handler = new() { MapInboundClaims = false };

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtLicenseTokenService(IOptions<JwtOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }

    public (string Token, DateTimeOffset ExpiresAt) Issue(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddHours(_options.LifetimeHours);
        if (license.ExpiresAt < expiresAt)
            expiresAt = license.ExpiresAt;

        // Una licencia vencida no debería llegar acá, pero el servicio no puede
        // emitir un token con exp anterior a nbf: se lo representa como emitido antes.
        var issuedAt = expiresAt <= now ? expiresAt.AddMinutes(-1) : now;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", license.Id.ToString()),
                new Claim("plan", license.Plan)
            ])
        };

        var token = new JwtSecurityTokenHandler().CreateToken(descriptor);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public bool TryValidate(string token, out LicenseClaims claims)
    {
        claims = default;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var principal = Handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = ClockSkew,
                    IssuerSigningKey = _signingKey
                },
                out var validated);

            var subject = principal.FindFirst("sub")?.Value;
            var plan = principal.FindFirst("plan")?.Value;

            // Doble control: no confiamos sólo en ValidateLifetime, porque un
            // token sin exp pasaría como válido para siempre.
            if (validated.ValidTo == DateTime.MinValue || validated.ValidTo <= DateTime.UtcNow)
                return false;

            if (!Guid.TryParse(subject, out var id) || string.IsNullOrWhiteSpace(plan))
                return false;

            claims = new LicenseClaims(
                id,
                plan,
                new DateTimeOffset(validated.ValidTo, TimeSpan.Zero));

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
