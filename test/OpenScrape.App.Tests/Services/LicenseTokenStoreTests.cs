using OpenScrape.App.Services.License;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La key y el token se guardan cifrados: el archivo no debe contener ninguno
/// de los dos en claro ni sobrevivir a una expiración.
/// </summary>
[TestFixture]
public class LicenseTokenStoreTests
{
    private string _path = null!;
    private LicenseTokenStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"lic-{Guid.NewGuid():N}.dat");
        _store = new LicenseTokenStore(_path, "secreto-de-prueba-para-cifrar");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Test]
    public void Save_YLoad_DevuelveLoMismo()
    {
        var original = new StoredLicense(
            "OSPK-1111-2222-3333",
            "abc.def.ghi",
            DateTimeOffset.UtcNow.AddHours(1),
            "pro");

        _store.Save(original);
        var loaded = _store.Load();

        Assert.That(loaded!.Key, Is.EqualTo(original.Key));
        Assert.That(loaded.Token, Is.EqualTo(original.Token));
        Assert.That(loaded.Plan, Is.EqualTo(original.Plan));
        Assert.That(loaded.ExpiresAt, Is.EqualTo(original.ExpiresAt));
    }

    [Test]
    public void Save_NoEscribeLaKeyNiElTokenEnClaro()
    {
        _store.Save(new StoredLicense(
            "OSPK-1111-2222-3333", "tok-secreto", DateTimeOffset.UtcNow.AddHours(1), "pro"));

        var raw = File.ReadAllText(_path);

        Assert.That(raw, Does.Not.Contain("OSPK-1111-2222-3333"));
        Assert.That(raw, Does.Not.Contain("tok-secreto"));
    }

    [Test]
    public void HasValidToken_SinArchivo_EsFalse()
    {
        Assert.That(_store.HasValidToken(DateTimeOffset.UtcNow), Is.False);
    }

    [Test]
    public void HasValidToken_Vencido_EsFalse()
    {
        _store.Save(new StoredLicense("k", "t", DateTimeOffset.UtcNow.AddMinutes(-1), "pro"));

        Assert.That(_store.HasValidToken(DateTimeOffset.UtcNow), Is.False);
    }

    [Test]
    public void Clear_BorraElArchivo()
    {
        _store.Save(new StoredLicense("k", "t", DateTimeOffset.UtcNow.AddHours(1), "pro"));

        _store.Clear();

        Assert.That(File.Exists(_path), Is.False);
        Assert.That(_store.Load(), Is.Null);
    }
}
