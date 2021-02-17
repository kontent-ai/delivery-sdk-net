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
        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public Lazy<Task<IList<object>>> LinkedItems { get; set; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        public Lazy<Task<dynamic>> LinkedItemsDynamic { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractItemsResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="linkedItems">Collection of linked content items.</param>
        protected AbstractItemsResponse(ApiResponse response, Func<Task<IList<object>>> linkedItems) : base(response)
        {
            LinkedItems = new Lazy<Task<IList<object>>>(async () => await linkedItems());
            LinkedItemsDynamic = new Lazy<Task<dynamic>>(async () =>
            {
                var content = await response.GetJsonContentAsync();
                return (JObject)content["modular_content"].DeepClone();
            });
        }
    }
}
