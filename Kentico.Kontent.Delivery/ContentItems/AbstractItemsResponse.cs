using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// Response object with built-in linked items resolution.
    /// </summary>
    internal abstract class AbstractItemsResponse : AbstractResponse
    {
        private Lazy<Task<IList<object>>> _linkedItems;
        private Lazy<Task<dynamic>> _linkedItemsDynamic;

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public Task<IList<object>> LinkedItems => _linkedItems.Value;

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public Task<dynamic> LinkedItemsDynamic => _linkedItemsDynamic.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractItemsResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="linkedItems">A delegate to resolve linked items.</param>
        protected AbstractItemsResponse(ApiResponse response, Func<Task<IList<object>>> linkedItems) : base(response)
        {
            _linkedItems = new Lazy<Task<IList<object>>>(async () => await linkedItems());
            _linkedItemsDynamic = new Lazy<Task<dynamic>>(async () =>
            {
                var content = await response.GetJsonContentAsync();
                return (JObject)content["modular_content"].DeepClone();
            });
        }
    }
}
