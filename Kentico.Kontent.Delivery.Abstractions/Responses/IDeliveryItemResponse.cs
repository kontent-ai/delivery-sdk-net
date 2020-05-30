namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryItemResponse<T> : IResponse
    {
        T Item { get; }
        dynamic LinkedItems { get; }
    }
}