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
            // MaxDepth matches the Kontent.ai API nesting limits (aligned with GraphQL API)
            MaxDepth = 124
        };

        // Register converters
        // ContentItemConverterFactory handles ContentItem and Elements processing inline
        jsonSerializerOptions.Converters.Add(new ContentItemConverterFactory());
        // ContentElementConverter handles polymorphic element type deserialization
        jsonSerializerOptions.Converters.Add(new ContentElementConverter());
        // ContentElementDictionaryConverter hydrates codename from dictionary keys
        jsonSerializerOptions.Converters.Add(new ContentElementDictionaryConverter());

        return jsonSerializerOptions;
    }
}
