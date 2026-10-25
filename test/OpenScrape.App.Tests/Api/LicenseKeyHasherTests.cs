using Microsoft.Extensions.Options;
using OpenScrape.Api.Licensing;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// El hash es la única forma de comparar una key sin guardarla en claro.
/// </summary>
[TestFixture]
public class LicenseKeyHasherTests
{
    [Test]
    public void Hash_MismaKeyYMismoPepper_EsEstable()
    {
        var hasher = new LicenseKeyHasher(Options.Create(new LicenseOptions { Pepper = "pimienta" }));

        Assert.That(
            hasher.Hash("OSPK-1111-2222-3333"),
            Is.EqualTo(hasher.Hash("OSPK-1111-2222-3333")));
    }

    [Test]
    public void Hash_PepperDistinto_CambiaElHash()
    {
        var a = new LicenseKeyHasher(Options.Create(new LicenseOptions { Pepper = "a" }))
            .Hash("OSPK-1111-2222-3333");
        var b = new LicenseKeyHasher(Options.Create(new LicenseOptions { Pepper = "b" }))
            .Hash("OSPK-1111-2222-3333");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Hash_NoContieneLaKeyEnClaro()
    {
        var hash = new LicenseKeyHasher(Options.Create(new LicenseOptions { Pepper = "p" }))
            .Hash("OSPK-1111-2222-3333");

        Assert.That(hash, Does.Not.Contain("OSPK"));
    }

    [Test]
    public void Create_GeneraElFormatoEsperadoYNoSeRepite()
    {
        var first = LicenseKeyGenerator.Create();
        var second = LicenseKeyGenerator.Create();

        Assert.That(first, Does.Match("^OSPK(-[A-Z2-9]{4}){3}$"));
        Assert.That(first, Is.Not.EqualTo(second));
    }
}
