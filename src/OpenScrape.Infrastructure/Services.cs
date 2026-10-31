using JasperFx;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Domain.Entities;

namespace OpenScrape.Infrastructure;

public static class Services
{
    public static void AddDataBase(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada.");
        }

        services.AddMarten(options =>
        {
            // Establish the connection string to your Marten database
            options.Connection(connectionString);

            // Specify that we want to use STJ as our serializer
            options.UseSystemTextJsonForSerialization();

            // El historial de sesiones y manos dejó de persistirse: sólo queda el rastro
            // de decisiones, que es diagnóstico del motor.
            options.Schema.For<DecisionTrace>().Index(x => x.Timestamp);
            options.Schema.For<DecisionTrace>().Index(x => x.VillainId);
            options.Schema.For<DecisionTrace>().Index(x => x.Street);

            // If we're running in development mode, let Marten just take care
            // of all necessary schema building and patching behind the scenes
            if (isDevelopment)
            {
                options.AutoCreateSchemaObjects = AutoCreate.All;
            }
        });
    }
}
