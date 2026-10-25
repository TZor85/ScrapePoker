using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Opciones de serialización equivalentes a las que configura la API:
/// camelCase para propiedades y enums como texto camelCase.
/// </summary>
internal static class JsonConfig
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
