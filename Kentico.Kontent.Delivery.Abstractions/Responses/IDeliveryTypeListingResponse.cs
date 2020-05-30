using Kentico.Kontent.Delivery.Abstractions.Models.Type;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryTypeListingResponse : IResponse
    {
        IPagination Pagination { get; }
        IReadOnlyList<IContentType> Types { get; }
    }
}