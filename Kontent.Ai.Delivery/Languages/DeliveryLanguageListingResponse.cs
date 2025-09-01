using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages
{
    /// <inheritdoc cref="IDeliveryLanguageListingResponse" />
    internal sealed class DeliveryLanguageListingResponse : IDeliveryLanguageListingResponse
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
        /// <param name="languages">A collection of languages.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryLanguageListingResponse(IList<ILanguage> languages, IPagination pagination)
        {
            Languages = languages;
            Pagination = pagination;
        }
    }
}
