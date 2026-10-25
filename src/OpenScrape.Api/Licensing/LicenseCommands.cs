namespace OpenScrape.Api.Licensing;

/// <summary>
/// Comandos de administración de licencias. Reciben sus dependencias y un
/// <see cref="TextWriter"/> para poder probarlos sin consola ni base real.
/// </summary>
public static class LicenseCommands
{
    public static async Task<int> CreateAsync(
        ILicenseStore store,
        ILicenseKeyHasher hasher,
        string plan,
        int days,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrWhiteSpace(plan))
            throw new ArgumentException("El plan es obligatorio.", nameof(plan));

        if (days <= 0)
            throw new ArgumentOutOfRangeException(nameof(days), "Los días deben ser positivos.");

        var key = LicenseKeyGenerator.Create();
        var now = DateTimeOffset.UtcNow;

        await store.AddAsync(
            new License
            {
                Id = Guid.NewGuid(),
                KeyHash = hasher.Hash(key),
                Plan = plan,
                ExpiresAt = now.AddDays(days),
                CreatedAt = now
            },
            cancellationToken);

        // La key se imprime una sola vez: no se guarda en claro en ningún lado.
        await output.WriteLineAsync(key);
        return 0;
    }

    public static async Task<int> RevokeAsync(
        ILicenseStore store,
        Guid id,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(output);

        var revoked = await store.RevokeAsync(id, cancellationToken);
        await output.WriteLineAsync(revoked ? $"Revocada {id}" : $"No existe {id}");

        return revoked ? 0 : 1;
    }

    public static async Task<int> ListAsync(
        ILicenseStore store,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(output);

        var licenses = await store.ListAsync(cancellationToken);

        await output.WriteLineAsync("ID  PLAN  VENCE  REVOCADA");

        foreach (var license in licenses)
        {
            await output.WriteLineAsync(
                $"{license.Id}  {license.Plan}  {license.ExpiresAt:yyyy-MM-dd}  {(license.Revoked ? "si" : "no")}");
        }

        return 0;
    }
}
