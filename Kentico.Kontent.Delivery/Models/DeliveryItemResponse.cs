using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace Kentico.Kontent.Delivery.Models
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content item.
    /// </summary>
    /// <typeparam name="T">The type of a content item in the response.</typeparam>
    public sealed class DeliveryItemResponse<T> : AbstractResponse, IDeliveryItemResponse<T>
    {
        private readonly IModelProvider _modelProvider;
        private readonly Lazy<T> _item;
        private readonly Lazy<JObject> _linkedItems;

        /// <summary>
        /// Gets the content item.
        /// </summary>
        public T Item => _item.Value;

        /// <summary>
        /// Gets the dynamic view of the JSON response where linked items and their properties can be retrieved by name, for example <c>LinkedItems.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="modelProvider">The provider that can convert JSON responses into instances of .NET types.</param>
        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _modelProvider = modelProvider;
            _item = new Lazy<T>(() => _modelProvider.GetContentItemModel<T>(response.JsonContent["item"], response.JsonContent["modular_content"]), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)response.JsonContent["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator T(DeliveryItemResponse<T> response) => response.Item;
    }
}
