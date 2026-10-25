using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenScrape.Api.Licensing;
using OpenScrape.Infrastructure;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    await Console.Out.WriteLineAsync("Uso:");
    await Console.Out.WriteLineAsync("  create --plan pro --days 365");
    await Console.Out.WriteLineAsync("  revoke --id <guid>");
    await Console.Out.WriteLineAsync("  list");
    return 0;
}

// Los argumentos de la CLI no son configuración del host: la conexión y el
// pepper llegan por variables de entorno (ConnectionStrings__DefaultConnection,
// License__Pepper).
var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
builder.Services.AddDataBase(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection("License"));
builder.Services.AddSingleton<ILicenseKeyHasher, LicenseKeyHasher>();
builder.Services.AddScoped<ILicenseStore, MartenLicenseStore>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var store = scope.ServiceProvider.GetRequiredService<ILicenseStore>();
var hasher = scope.ServiceProvider.GetRequiredService<ILicenseKeyHasher>();

try
{
    return args[0] switch
    {
        "create" => await LicenseCommands.CreateAsync(
            store,
            hasher,
            Option(args, "--plan") ?? "pro",
            int.Parse(Option(args, "--days") ?? "365"),
            Console.Out),
        "revoke" => await LicenseCommands.RevokeAsync(
            store,
            Guid.Parse(Option(args, "--id") ?? string.Empty),
            Console.Out),
        "list" => await LicenseCommands.ListAsync(store, Console.Out),
        _ => Usage()
    };
}
catch (Exception ex) when (ex is FormatException or ArgumentException or ArgumentOutOfRangeException)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 2;
}

static string? Option(string[] arguments, string name)
{
    var index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

static int Usage()
{
    Console.Error.WriteLine("Comando desconocido. Probar --help.");
    return 2;
}
