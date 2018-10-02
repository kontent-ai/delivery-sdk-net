using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    /// <typeparam name="T">Generic strong type of item representation.</typeparam>
    public sealed class DeliveryItemListingResponse<T> : AbstractResponse
    {
        private readonly JToken _response;
        private readonly ICodeFirstModelProvider _codeFirstModelProvider;
        private dynamic _modularContent;
        private Pagination _pagination;
        private IReadOnlyList<T> _items;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination
        {
            get { return _pagination ?? (_pagination = _response["pagination"].ToObject<Pagination>()); }
        }

        /// <summary>
        /// Gets a list of content items.
        /// </summary>
        public IReadOnlyList<T> Items
        {
            get { return _items ?? (_items = ((JArray)_response["items"]).Select(source => _codeFirstModelProvider.GetContentItemModel<T>(source, _response["modular_content"])).ToList().AsReadOnly()); }
        }


        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a list of content items.</param>
        /// <param name="codeFirstModelProvider"></param>
        /// <param name="apiUrl">API URL used to communicate with the underlying Kentico Cloud endpoint.</param>
        internal DeliveryItemListingResponse(JToken response, ICodeFirstModelProvider codeFirstModelProvider, string apiUrl) : base(apiUrl)
        {
            _response = response;
            _codeFirstModelProvider = codeFirstModelProvider;
        }
    }
}
