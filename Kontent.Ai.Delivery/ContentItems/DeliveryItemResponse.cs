using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemResponse{TModel}" />
    internal sealed record DeliveryItemResponse<TModel> : IDeliveryItemResponse<TModel>
        where TModel : IElementsModel
    {
        /// <inheritdoc/>
        [JsonPropertyName("item")]
        public required IContentItem<TModel> Item { get; init; }

        /// <summary>
        /// Raw modular content used for resolving linked items/inline content.
        /// </summary>
        [JsonPropertyName("modular_content")]
        internal required Dictionary<string, JsonElement> ModularContent { get; init; } = [];
    }
}
