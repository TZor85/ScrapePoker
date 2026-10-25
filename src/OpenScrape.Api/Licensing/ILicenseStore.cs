namespace OpenScrape.Api.Licensing;

/// <summary>Acceso a las licencias. La implementación real vive sobre Marten.</summary>
public interface ILicenseStore
{
    Task<License?> FindByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);

    Task<License?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(License license, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<License>> ListAsync(CancellationToken cancellationToken = default);
}
