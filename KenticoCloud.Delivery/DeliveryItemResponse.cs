using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a content item.
    /// </summary>
    public sealed class DeliveryItemResponse
    {
        private readonly JToken _response;
        private dynamic _modularContent;
        private ContentItem _item;

        /// <summary>
        /// Gets the content item from the response.
        /// </summary>
        public ContentItem Item
        {
            get { return _item ?? (_item = new ContentItem(_response["item"], _response["modular_content"])); }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a content item.</param>
        internal DeliveryItemResponse(JToken response)
        {
            _response = response;
        }

        /// <summary>
        /// Casts the response to a response with a strongly-typed model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DeliveryItemResponse<T> CastTo<T>()
        {
            return new DeliveryItemResponse<T>(_response);
        }
    }


    /// <summary>
    /// Represents a response from the API when requesting content item by its codename.
    /// </summary>
    public sealed class DeliveryItemResponse<T>
    {
        private readonly JToken _response;
        private dynamic _modularContent;
        private T _item;

        /// <summary>
        /// Content item.
        /// </summary>
        public T Item
        {
            get
            {
                if (_item == null)
                {
                    _item = ContentItem.Parse<T>(_response["item"], _response["modular_content"]);
                }
                return _item;
            }
        }

        /// <summary>
        /// Modular content.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        /// <summary>
        /// Initializes response object with a JSON response.
        /// </summary>
        /// <param name="response">JSON returned from API.</param>
        internal DeliveryItemResponse(JToken response)
        {
            _response = response;
        }
    }
}