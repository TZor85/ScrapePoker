using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Infrastructure;

namespace OpenScrape.App.Tests;

[TestFixture]
public class InfrastructureServicesTests
{
    [Test]
    public void AddDataBase_SinDefaultConnection_LanzaErrorAccionable()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddDataBase(configuration, isDevelopment: false));

        Assert.That(ex!.Message, Does.Contain("DefaultConnection"));
    }
}
