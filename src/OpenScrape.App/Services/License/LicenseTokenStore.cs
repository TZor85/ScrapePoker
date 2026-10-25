using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenScrape.App.Helpers;

namespace OpenScrape.App.Services.License;

/// <summary>Licencia tal como se persiste en el disco del cliente.</summary>
public sealed record StoredLicense(
    string Key,
    string Token,
    DateTimeOffset ExpiresAt,
    string Plan);

/// <summary>
/// Guarda la key y el token cifrados con <see cref="EncrypterHelper"/>. El
/// archivo no contiene ninguno de los dos en claro.
/// </summary>
public sealed class LicenseTokenStore(string filePath, string secret)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _filePath = string.IsNullOrWhiteSpace(filePath)
        ? throw new ArgumentException("Falta la ruta del archivo de licencia.", nameof(filePath))
        : filePath;

    private readonly byte[] _key = SHA256.HashData(
        Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(secret)
                ? throw new ArgumentException("Falta el secreto de cifrado.", nameof(secret))
                : secret));

    private readonly byte[] _iv = MD5.HashData(Encoding.UTF8.GetBytes(secret))[..16];

    public StoredLicense? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var cipher = File.ReadAllText(_filePath);
            var json = EncrypterHelper.Decrypt(cipher, _key, _iv);
            return JsonSerializer.Deserialize<StoredLicense>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or JsonException or FormatException)
        {
            // Un archivo corrupto equivale a no tener licencia.
            return null;
        }
    }

    public void Save(StoredLicense license)
    {
        ArgumentNullException.ThrowIfNull(license);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(license, JsonOptions);
        File.WriteAllText(_filePath, EncrypterHelper.Encrypt(json, _key, _iv));
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    public bool HasValidToken(DateTimeOffset now) =>
        Load() is { Token.Length: > 0 } stored && stored.ExpiresAt > now;
}
