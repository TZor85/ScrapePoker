using OpenScrape.Api.Licensing;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Los comandos de la CLI son lógica pura: reciben el store y el hasher, y
/// escriben en un TextWriter. La consola sólo los cablea.
/// </summary>
[TestFixture]
public class LicenseCliTests
{
    private static readonly LicenseKeyHasher Hasher = LicenseTestData.CreateHasher();

    [Test]
    public async Task Create_ImprimeLaKeyUnaSolaVezYGuardaElHash()
    {
        var store = LicenseTestData.CreateStore();
        var output = new StringWriter();

        var exit = await LicenseCommands.CreateAsync(store, Hasher, "pro", 30, output);

        var key = output.ToString().Trim();
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(key, Does.Match("^OSPK(-[A-Z0-9]{4}){3}$"));
        Assert.That(key, Does.Not.Contain(" "));
        Assert.That(await store.FindByKeyHashAsync(Hasher.Hash(key)), Is.Not.Null);
    }

    [Test]
    public async Task Revoke_IdInexistente_DevuelveUno()
    {
        var exit = await LicenseCommands.RevokeAsync(
            LicenseTestData.CreateStore(), Guid.NewGuid(), new StringWriter());

        Assert.That(exit, Is.EqualTo(1));
    }

    [Test]
    public async Task List_SinLicencias_EscribeElEncabezado()
    {
        var store = new InMemoryLicenseStore();
        var output = new StringWriter();

        var exit = await LicenseCommands.ListAsync(store, output);

        Assert.That(exit, Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("PLAN"));
        Assert.That(output.ToString(), Does.Contain("VENCE"));
    }
}
