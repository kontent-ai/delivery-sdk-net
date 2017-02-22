using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content types.
    /// </summary>
    public sealed class DeliveryTypeListingResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination { get; }

        /// <summary>
        /// Gets a list of content types.
        /// </summary>
        public IReadOnlyList<ContentType> Types { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a list of content types.</param>
        internal DeliveryTypeListingResponse(JToken response)
        {
            Pagination = new Pagination(response["pagination"]);
            Types = ((JArray)response["types"]).Select(type => new ContentType(type)).ToList().AsReadOnly();
        }
    }
}
