using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryElementResponse : IResponse
    {
        IContentElement Element { get; }
    }
}