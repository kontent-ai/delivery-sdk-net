using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element
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
        IReadOnlyList<IMultipleChoiceOption> Options { get; }

        /// <summary>
        /// Gets the codename of the taxonomy group for the Taxonomy content element; otherwise, an empty string.
        /// </summary>
        string TaxonomyGroup { get; }

        /// <summary>
        /// Gets the type of the content element, for example "multiple_choice".
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Gets the value of the content element.
        /// </summary>
        string Value { get; }
    }
}