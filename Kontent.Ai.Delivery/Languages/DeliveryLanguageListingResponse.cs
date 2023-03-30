using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Languages
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
        /// <param name="response">The response from Kontent.ai Delivery API that contains languages.</param>
        /// <param name="languages">A collection of languages.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryLanguageListingResponse(IApiResponse response, IList<ILanguage> languages, IPagination pagination) : base(response)
        {
            Languages = languages;
            Pagination = pagination;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryLanguageListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains languages.</param>
        internal DeliveryLanguageListingResponse(IApiResponse response) : base(response)
        {
        }
    }
}
