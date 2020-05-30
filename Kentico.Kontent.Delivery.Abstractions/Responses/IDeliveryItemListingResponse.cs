using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryItemListingResponse<T> : IResponse
    {
        IReadOnlyList<T> Items { get; }
        dynamic LinkedItems { get; }
        IPagination Pagination { get; }
    }
}