using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace OpenScrape.Api.Licensing;

/// <summary>
/// Falla el arranque si la configuración de licencias no sirve. Es preferible
/// un servicio que no levanta a uno que firma tokens con una key de 3 letras.
/// </summary>
public sealed class LicenseStartupValidation(
    IOptions<JwtOptions> jwtOptions,
    IOptions<LicenseOptions> licenseOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var signingKey = jwtOptions.Value.SigningKey;

        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey es obligatorio y debe tener al menos 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(licenseOptions.Value.Pepper))
            throw new InvalidOperationException("License:Pepper es obligatorio.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
