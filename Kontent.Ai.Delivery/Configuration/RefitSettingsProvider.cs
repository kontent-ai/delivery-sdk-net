using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// Provides default Refit settings for the Kontent.ai Delivery SDK.
/// </summary>
public static class RefitSettingsProvider
{
    /// <summary>
    /// Creates default Refit settings configured for Kontent.ai Delivery API.
    /// </summary>
    /// <returns>Configured RefitSettings instance.</returns>
    public static RefitSettings CreateDefaultSettings()
    {
        var jsonSerializerOptions = CreateDefaultJsonSerializerOptions();

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
            CollectionFormat = CollectionFormat.Multi,
            UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter()
        };
    }

    /// <summary>
    /// Creates shared System.Text.Json options used across the SDK (Refit and internal mappers).
    /// </summary>
    public static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Register converters
        jsonSerializerOptions.Converters.Add(new ElementsConverterFactory());
        jsonSerializerOptions.Converters.Add(new ContentItemConverterFactory());
        return jsonSerializerOptions;
    }
}
