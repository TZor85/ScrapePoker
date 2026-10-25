using Marten;

namespace OpenScrape.Api.Licensing;

/// <summary>Licencias persistidas en Marten, esquema <c>license</c>.</summary>
public sealed class MartenLicenseStore(IDocumentStore store) : ILicenseStore
{
    private readonly IDocumentStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public async Task<License?> FindByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        return await session.Query<License>()
            .FirstOrDefaultAsync(license => license.KeyHash == keyHash, cancellationToken);
    }

    public async Task<License?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        return await session.LoadAsync<License>(id, cancellationToken);
    }

    public async Task AddAsync(License license, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        session.Store(license);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        var license = await session.LoadAsync<License>(id, cancellationToken);
        if (license is null)
            return false;

        license.Revoked = true;
        session.Store(license);
        await session.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<License>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();
        return await session.Query<License>().ToListAsync(cancellationToken);
    }
}
