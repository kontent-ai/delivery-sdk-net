using Kontent.Ai.Delivery.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Newtonsoft.Json.Linq;
using Kontent.Ai.Urls.Delivery;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Extensions.Universal
{
    public static class DeliveryClientExtensions
    {
        public static async Task<IDeliveryUniversalItemResponse> GetUniversalItemAsync(this IDeliveryClient client, string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            var endpointUrl = new DeliveryEndpointUrlBuilder(client.DeliveryOptions).GetItemUrl(codename, parameters);
            var response = await client.GetDeliveryResponseAsync(endpointUrl);

            if (!response.IsSuccess)
            {
                return new DeliveryUniversalItemResponse(response);
            }

            var content = (JObject)await response.GetJsonContentAsync();
            var model = await UniversalContentItemModelProvider.GetContentItemGenericModelAsync(content["item"], (JsonSerializer)client.Serializer);

            var linkedUniversalItems = await Task.WhenAll(
                content["modular_content"]?
                .Values()
                .Select(async linkedItem =>
                {
                    var model = await UniversalContentItemModelProvider.GetContentItemGenericModelAsync(linkedItem, (JsonSerializer)client.Serializer);
                    return new KeyValuePair<string, IUniversalContentItem>(model.System.Codename, model);
                })
            );

            return new DeliveryUniversalItemResponse(
                response,
                model,
                linkedUniversalItems.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public static async Task<IDeliveryUniversalItemListingResponse> GetUniversalItemsAsync(this IDeliveryClient client, IEnumerable<IQueryParameter> parameters = null)
        {
            var endpointUrl = new DeliveryEndpointUrlBuilder(client.DeliveryOptions).GetItemsUrl(parameters);
            var response = await client.GetDeliveryResponseAsync(endpointUrl);

            if (!response.IsSuccess)
            {
                return new DeliveryUniversalItemListingResponse(response);
            }

            var content = (JObject)await response.GetJsonContentAsync();
            var pagination = content["pagination"].ToObject<Pagination>((JsonSerializer)client.Serializer);

            var items = ((JArray)content["items"]).Select(async source => await UniversalContentItemModelProvider.GetContentItemGenericModelAsync(source, (JsonSerializer)client.Serializer));

            var linkedUniversalItems = await Task.WhenAll(
                content["modular_content"]?
                .Values()
                .Select(async linkedItem =>
                {
                    var model = await UniversalContentItemModelProvider.GetContentItemGenericModelAsync(linkedItem, (JsonSerializer)client.Serializer);
                    return new KeyValuePair<string, IUniversalContentItem>(model.System.Codename, model);
                })
            );

            return new DeliveryUniversalItemListingResponse(
                response,
                (await Task.WhenAll(items)).ToList(),
                pagination,
                linkedUniversalItems.ToDictionary(pair => pair.Key, pair => pair.Value)
                );
        }

    }
}