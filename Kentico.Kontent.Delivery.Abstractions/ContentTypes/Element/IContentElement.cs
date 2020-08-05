using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a content element.
    /// </summary>
    public interface IContentElement
    {
        /// <summary>
        /// Gets the codename of the content element.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the name of the content element.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a list of predefined options for the Multiple choice content element; otherwise, an empty list.
        /// </summary>
        IList<IMultipleChoiceOption> Options { get; }  //TODO: move to a specific CE type

        /// <summary>
        /// Gets the codename of the taxonomy group for the Taxonomy content element; otherwise, an empty string.
        /// </summary>
        string TaxonomyGroup { get; }  //TODO: move to a specific CE type

        /// <summary>
        /// Gets the type of the content element, for example "multiple_choice".
        /// </summary>
        string Type { get; }
    }
}