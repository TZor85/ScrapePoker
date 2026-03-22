using JasperFx;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Domain.Entities;

namespace OpenScrape.Infrastructure;

public static class Services
{
    public static void AddDataBase(this IServiceCollection services, IConfiguration configuration, bool IsDevelopment)
    {
        services.AddMarten(options =>
        {
            // Establish the connection string to your Marten database
            options.Connection(configuration.GetConnectionString("DefaultConnection")!);

            // Specify that we want to use STJ as our serializer
            options.UseSystemTextJsonForSerialization();

            // Índices para GameSession — acelera consultas por fecha y por sesión
            options.Schema.For<GameSession>().Index(x => x.EndTime);
            options.Schema.For<GameSession>().Index(x => x.SessionId);

            // Índices para HandRecord — acelera consultas por sesión y por fecha
            options.Schema.For<HandRecord>().Index(x => x.GameSessionId);
            options.Schema.For<HandRecord>().Index(x => x.Timestamp);

            // If we're running in development mode, let Marten just take care
            // of all necessary schema building and patching behind the scenes
            if (IsDevelopment)
            {
                options.AutoCreateSchemaObjects = AutoCreate.All;
            }
        });
    }
}
