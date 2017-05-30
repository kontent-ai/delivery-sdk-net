using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains an content items.
    /// </summary>
    /// <typeparam name="T">Generic strong type of item representation.</typeparam>
    public sealed class DeliveryItemResponse<T>
    {
        private readonly JToken _response;
        private readonly DeliveryClient _client;
        private dynamic _modularContent;
        private T _item;

        /// <summary>
        /// Gets a content item.
        /// </summary>
        public T Item
        {
            get {
                if (_item == null)
                {
                    _item = _client.CodeFirstModelProvider.GetContentItemModel<T>(_response["item"], _response["modular_content"]);
                }
                return _item;
            }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        internal DeliveryItemResponse(JToken response, DeliveryClient client)
        {
            _response = response;
            _client = client;
        }
    }
}
