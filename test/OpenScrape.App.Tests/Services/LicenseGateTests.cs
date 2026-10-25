using OpenScrape.App.Services.License;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La compuerta de arranque: sin token vigente el motor remoto no debe arrancar.
/// </summary>
[TestFixture]
public class LicenseGateTests
{
    private string _path = null!;
    private LicenseTokenStore _store = null!;
    private readonly DateTimeOffset _now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"gate-{Guid.NewGuid():N}.dat");
        _store = new LicenseTokenStore(_path, "secreto-de-prueba");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Test]
    public void NeedsActivation_SinArchivo_EsTrue()
    {
        Assert.That(LicenseGate.NeedsActivation(_store, _now), Is.True);
    }

    [Test]
    public void NeedsActivation_TokenVigente_EsFalse()
    {
        _store.Save(new StoredLicense("k", "token", _now.AddHours(1), "pro"));

        Assert.That(LicenseGate.NeedsActivation(_store, _now), Is.False);
    }

    [Test]
    public void NeedsActivation_TokenVencido_EsTrue()
    {
        _store.Save(new StoredLicense("k", "token", _now.AddMinutes(-1), "pro"));

        Assert.That(LicenseGate.NeedsActivation(_store, _now), Is.True);
    }

    [Test]
    public void NeedsActivation_ConKeyPeroSinToken_EsTrue()
    {
        _store.Save(new StoredLicense("OSPK-1111-2222-3333", string.Empty, _now.AddHours(1), "pro"));

        Assert.That(LicenseGate.NeedsActivation(_store, _now), Is.True);
    }
}
