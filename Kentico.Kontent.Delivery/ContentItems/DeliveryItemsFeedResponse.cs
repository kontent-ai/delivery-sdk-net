using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
    public class DeliveryItemsFeedResponse<T> : AbstractItemsResponse, IEnumerable<T>, IDeliveryItemsFeedResponse<T>
    {
        private readonly Lazy<IReadOnlyList<T>> _items;

        /// <inheritdoc/>
        public IReadOnlyList<T> Items => _items.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemsFeedResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content items.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemsFeedResponse(ApiResponse response, IModelProvider modelProvider) : base(response, modelProvider)
        {
            _items = new Lazy<IReadOnlyList<T>>(() => ((JArray)response.JsonContent["items"]).Select(source => modelProvider.GetContentItemModel<T>(source, response.JsonContent["modular_content"])).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of content items.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}