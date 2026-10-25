namespace OpenScrape.App.Services.License;

/// <summary>
/// Decide si el cliente necesita activar la licencia antes de arrancar el motor.
/// </summary>
public static class LicenseGate
{
    public static bool NeedsActivation(LicenseTokenStore store, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(store);

        return !store.HasValidToken(now);
    }
}
