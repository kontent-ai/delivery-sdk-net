using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc cref="IDeliveryElementResponse" />
[DebuggerDisplay("Name = {" + nameof(Element) + "." + nameof(IContentElement.Name) + "}")]
internal sealed class DeliveryElementResponse : IDeliveryElementResponse
{
    /// <inheritdoc/>
    public IContentElement Element
    {
        get;
        private set;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryElementResponse"/> class.
    /// </summary>
    /// <param name="element">A content element.</param>
    [JsonConstructor]
    internal DeliveryElementResponse(IContentElement element)
    {
        Element = element;
    }
}
