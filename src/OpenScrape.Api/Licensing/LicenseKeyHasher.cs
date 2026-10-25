using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OpenScrape.Api.Licensing;

/// <summary>Deriva el identificador que se guarda en la base a partir de la key.</summary>
public interface ILicenseKeyHasher
{
    string Hash(string key);
}

/// <summary>SHA-256 de <c>pepper + key</c>, en hexadecimal minúscula.</summary>
public sealed class LicenseKeyHasher(IOptions<LicenseOptions> options) : ILicenseKeyHasher
{
    private readonly string _pepper = options?.Value.Pepper
        ?? throw new ArgumentNullException(nameof(options));

    public string Hash(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bytes = Encoding.UTF8.GetBytes(_pepper + key);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}

/// <summary>Genera keys legibles, sin caracteres que se confunden al tipear.</summary>
public static class LicenseKeyGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create()
    {
        Span<char> key = stackalloc char[19];
        "OSPK-".AsSpan().CopyTo(key);

        for (var i = 5; i < key.Length; i++)
        {
            if ((i - 5) % 5 == 4)
            {
                key[i] = '-';
                continue;
            }

            key[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(key);
    }
}
