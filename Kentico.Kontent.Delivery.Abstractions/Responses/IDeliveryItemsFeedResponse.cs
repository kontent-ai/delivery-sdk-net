using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryItemsFeedResponse<T> : IResponse
    {
        IReadOnlyList<T> Items { get; }
        dynamic LinkedItems { get; }

        IEnumerator<T> GetEnumerator();
    }
}