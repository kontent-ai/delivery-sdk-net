using System.Text.Json;
using Kontent.Ai.Delivery.Serialization.Converters;

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
            PropertyNameCaseInsensitive = true,
            MaxDepth = 124 // limit set to the same value as is GraphQL API limit
            // TODO: confirm depth for rest api
        };

        // Register converters
        // ContentItemConverterFactory now handles both ContentItem and Elements processing inline
        jsonSerializerOptions.Converters.Add(new ContentItemConverterFactory());

        return jsonSerializerOptions;
    }
}