using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">The type of a content item in the response.</typeparam>
    public sealed class DeliveryItemResponse<T> : AbstractResponse
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

        internal DeliveryItemResponse(ApiResponse response, IModelProvider modelProvider) : base(response)
        {
            _modelProvider = modelProvider;
            _item = new Lazy<T>(() => _modelProvider.GetContentItemModel<T>(_response.Content["item"], _response.Content["modular_content"]), LazyThreadSafetyMode.PublicationOnly);
            _linkedItems = new Lazy<JObject>(() => (JObject)_response.Content["modular_content"].DeepClone(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
