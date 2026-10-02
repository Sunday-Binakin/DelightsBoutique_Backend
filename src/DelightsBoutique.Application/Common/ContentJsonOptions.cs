using System.Text.Json;
using System.Text.Json.Serialization;

namespace DelightsBoutique.Application.Common;

/// <summary>Shared options for every content (de)serialize call, so casing (and enum
/// representation) stays consistent with how the Api layer serializes everything else.</summary>
public static class ContentJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
