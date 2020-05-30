using Kentico.Kontent.Delivery.Abstractions.Models.Type;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryTypeResponse : IResponse
    {
        IContentType Type { get; }
    }
}