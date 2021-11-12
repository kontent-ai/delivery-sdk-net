using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.Languages
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
