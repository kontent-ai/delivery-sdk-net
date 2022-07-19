using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent Delivery API that contains a list of languages.
    /// </summary>
    public interface IDeliveryLanguageListingResponse : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of languages.
        /// </summary>
        IList<ILanguage> Languages { get; }
    }
}
