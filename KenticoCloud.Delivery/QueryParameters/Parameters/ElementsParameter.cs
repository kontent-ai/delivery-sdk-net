using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies the content elements to retrieve.
    /// </summary>
    public sealed class ElementsParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the list of codenames of content elements that should be retrieved.
        /// </summary>
        public IReadOnlyList<string> ElementCodenames { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementsParameter"/> class using the specified content elements codenames.
        /// </summary>
        /// <param name="elementCodenames">An array that contains zero or more codenames of the content elements that should be retrieved.</param>
        public ElementsParameter(params string[] elementCodenames)
        {
            ElementCodenames = elementCodenames.ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("elements={0}", Uri.EscapeDataString(string.Join(",", ElementCodenames)));
        }
    }
}
