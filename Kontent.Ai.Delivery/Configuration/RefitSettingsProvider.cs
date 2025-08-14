using System.Text.Json;

namespace Kontent.Ai.Delivery.Configuration
{
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
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            return new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
                CollectionFormat = CollectionFormat.Multi,
                UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter()
            };
        }
    }
}