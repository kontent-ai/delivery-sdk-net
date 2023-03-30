using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Universal
{
    internal sealed class DeliveryUniversalItemListingResponse : AbstractResponse, IDeliveryUniversalItemListingResponse
    {
        public IList<IUniversalContentItem> Items { get; }

        public Dictionary<string, IUniversalContentItem> LinkedItems { get; }


        public IPagination Pagination { get; }

        public DeliveryUniversalItemListingResponse(IApiResponse response) : base(response)
        {
            ApiResponse = response;
        }

        [JsonConstructor]
        internal DeliveryUniversalItemListingResponse(IApiResponse response, IList<IUniversalContentItem> items, IPagination pagination, Dictionary<string, IUniversalContentItem> linkedItems = null) : this(response)
        {
            Items = items;
            Pagination = pagination;
            LinkedItems = linkedItems;
        }
    }
}