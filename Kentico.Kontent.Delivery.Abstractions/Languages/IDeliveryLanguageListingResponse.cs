using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of languages.
    /// </summary>
    public interface IDeliveryLanguageListingResponse : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of languages.
        /// </summary>
        IList<ILanguage> Languages { get; }
    }
}
