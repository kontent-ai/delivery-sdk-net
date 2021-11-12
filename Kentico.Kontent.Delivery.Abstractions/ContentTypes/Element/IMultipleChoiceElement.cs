using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element
{
    interface IMultipleChoiceElement : IContentElement
    {
        /// <summary>
        /// Gets a list of predefined options for the Multiple choice content element; otherwise, an empty list.
        /// </summary>
        IList<IMultipleChoiceOption> Options { get; }
    }
}
