using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Languages
{
    /// <inheritdoc cref="IDeliveryLanguageListingResponse" />
    internal sealed class DeliveryLanguageListingResponse : AbstractResponse, IDeliveryLanguageListingResponse
    {
        /// <inheritdoc/>
        public IList<ILanguage> Languages
        {
            get;
        }

        /// <inheritdoc/>
        public IPagination Pagination
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryLanguageListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains languages.</param>
        /// <param name="languages">A collection of languages.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryLanguageListingResponse(ApiResponse response, IList<ILanguage> languages, IPagination pagination) : base(response)
        {
            Languages = languages;
            Pagination = pagination;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryLanguageListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains languages.</param>
        [JsonConstructor]
        internal DeliveryLanguageListingResponse(ApiResponse response) : base(response)
        {
        }
    }
}
