using JasperFx;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            //FK
            //options.Schema.For<Domain.Entities.ProductSpecification>().ForeignKey<Domain.Entities.Product>(x => x.ProductId);
            //options.Schema.For<Domain.Entities.Product>().ForeignKey<Domain.Entities.Provider>(x => x.ProviderId);

            // Indexes                        
            //options.Schema.For<Domain.Entities.ProductSpecification>().Index(x => x.CountryCode);

            // If we're running in development mode, let Marten just take care
            // of all necessary schema building and patching behind the scenes
            if (IsDevelopment)
            {
                options.AutoCreateSchemaObjects = AutoCreate.All;
            }
        });
    }
}
