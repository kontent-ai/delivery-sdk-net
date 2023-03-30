using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.ContentItems.Universal
{
    internal class DeliveryUniversalItemResponse : AbstractResponse, IDeliveryUniversalItemResponse
    {
        public IUniversalContentItem Item { get; }

        public Dictionary<string, IUniversalContentItem> LinkedItems { get; }

        public DeliveryUniversalItemResponse(IApiResponse response) : base(response)
        {
            ApiResponse = response;
        }

        public DeliveryUniversalItemResponse(IApiResponse response, IUniversalContentItem item, Dictionary<string, IUniversalContentItem> linkedItems = null) : this(response)
        {
            Item = item;
            LinkedItems = linkedItems;
        }
    }
}