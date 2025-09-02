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
    }
}
