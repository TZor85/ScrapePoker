using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// Guarda de regresión del registro de servicios del cliente.
/// <para>
/// El cliente valida el contenedor con <c>ValidateOnBuild</c>: si un registro del
/// cliente desaparece, la app muere al arrancar sin mostrar ventana. Hasta ahora
/// ningún test construía ese contenedor, así que un borrado accidental de registros
/// pasaba el build y explotaba recién en la máquina del usuario. Este test lo
/// detecta antes.
/// </para>
/// </summary>
[TestFixture]
public class ClientServiceProviderTests
{
    [Test]
    public void ConfigureClientServices_ConstruyeUnContenedorCompleto()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Cadena sintácticamente válida: el test no se conecta a ninguna base.
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=openscrape_test;Username=test;Password=test",
                ["Encrypter:Key"] = "0123456789abcdef0123456789abcdef",
                ["StrategyProfile:Name"] = "Test",
                // AddLicense construye un HttpClient con BaseAddress: necesita un Uri válido.
                ["Api:BaseUrl"] = "http://localhost:5000"
            })
            .Build();

        var context = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = configuration,
            HostingEnvironment = new TestHostingEnvironment
            {
                EnvironmentName = Environments.Development
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        // Host.CreateDefaultBuilder arma el logging (incluido el provider del TextBox
        // que FrmMain consume) antes de invocar ConfigureServices; el test reproduce
        // ese baseline para validar sólo el registro del cliente.
        services.AddLogging(lb => lb.AddTextBoxLogger());

        Program.ConfigureClientServices(context, services);

        ServiceProvider? provider = null;

        Assert.DoesNotThrow(() => provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = false
        }));

        Assert.That(provider, Is.Not.Null);

        provider!.Dispose();
    }

    /// <summary>
    /// <c>HostingEnvironment</c> de Microsoft.Extensions.Hosting es interno, así que
    /// el test usa la implementación mínima de <see cref="IHostEnvironment"/>.
    /// </summary>
    private sealed class TestHostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "OpenScrape.App.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
