using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    interface IMultipleChoiceElement : IContentElement
    {
        /// <summary>
        /// Gets a list of predefined options for the Multiple choice content element; otherwise, an empty list.
        /// </summary>
        IList<IMultipleChoiceOption> Options { get; }
    }
}
