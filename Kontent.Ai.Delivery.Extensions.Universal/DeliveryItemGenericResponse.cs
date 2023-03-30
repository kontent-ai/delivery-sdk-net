using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Extensions.Universal
{
    internal class DeliveryUniversalItemResponse : IResponse, IDeliveryUniversalItemResponse
    {
        public IUniversalContentItem Item { get; }

        public Dictionary<string, IUniversalContentItem> LinkedItems { get; }

        public IApiResponse ApiResponse { get; }

        public DeliveryUniversalItemResponse(IApiResponse response)
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