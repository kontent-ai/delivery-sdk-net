using System;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// Response object with built-in linked items resolution.
    /// </summary>
    internal abstract class AbstractItemsResponse : AbstractResponse
    {
        private Lazy<dynamic> _linkedItems;

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public dynamic LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractItemsResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        protected AbstractItemsResponse(ApiResponse response) : base(response)
        {
            _linkedItems = new Lazy<dynamic>(() =>
            {
                var content = JObject.Parse(ApiResponse.Content ?? "{}");
                var modularContent = (JObject)content["modular_content"].DeepClone();
                return modularContent;
            });
        }
    }
}
