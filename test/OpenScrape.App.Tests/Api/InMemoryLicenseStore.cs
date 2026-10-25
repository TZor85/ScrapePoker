using OpenScrape.Api.Licensing;

namespace OpenScrape.App.Tests.Api;

/// <summary>Store en memoria: los tests de la API no necesitan Postgres.</summary>
internal sealed class InMemoryLicenseStore : ILicenseStore
{
    private readonly List<License> _licenses = [];

    public InMemoryLicenseStore(params License[] seed) => _licenses.AddRange(seed);

    public Task<License?> FindByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(_licenses.FirstOrDefault(license => license.KeyHash == keyHash));

    public Task<License?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_licenses.FirstOrDefault(license => license.Id == id));

    public Task AddAsync(License license, CancellationToken cancellationToken = default)
    {
        _licenses.Add(license);
        return Task.CompletedTask;
    }

    public Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var license = _licenses.FirstOrDefault(item => item.Id == id);
        if (license is null)
            return Task.FromResult(false);

        license.Revoked = true;
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<License>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<License>>(_licenses.ToList());
}

/// <summary>Datos compartidos por los tests de licencias.</summary>
internal static class LicenseTestData
{
    public const string Pepper = "pepper-de-prueba";
    public const string SigningKey = "clave-de-firma-de-prueba-con-32-bytes-01";
    public const string ValidKey = "OSPK-PRO0-PRO0-PRO0";
    public const string FreeKey = "OSPK-FREE-FREE-FREE";

    public static Guid ProLicenseId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static Guid FreeLicenseId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static InMemoryLicenseStore CreateStore()
    {
        var hasher = CreateHasher();
        var now = DateTimeOffset.UtcNow;

        return new InMemoryLicenseStore(
            new License
            {
                Id = ProLicenseId,
                KeyHash = hasher.Hash(ValidKey),
                Plan = "pro",
                ExpiresAt = now.AddDays(365),
                CreatedAt = now
            },
            new License
            {
                Id = FreeLicenseId,
                KeyHash = hasher.Hash(FreeKey),
                Plan = "free",
                ExpiresAt = now.AddDays(365),
                CreatedAt = now
            });
    }

    public static LicenseKeyHasher CreateHasher() =>
        new(Microsoft.Extensions.Options.Options.Create(new LicenseOptions { Pepper = Pepper }));
}
