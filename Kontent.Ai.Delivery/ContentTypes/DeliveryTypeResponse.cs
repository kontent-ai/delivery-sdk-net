using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc cref="IDeliveryTypeResponse" />
internal sealed class DeliveryTypeResponse : IDeliveryTypeResponse
{
    /// <inheritdoc/>
    public IContentType Type
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
    /// </summary>
    /// <param name="type">A content type.</param>
    [JsonConstructor]
    internal DeliveryTypeResponse(IContentType type)
    {
        Type = type;
    }
}