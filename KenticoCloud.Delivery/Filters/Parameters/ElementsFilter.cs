using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents "elements" query parameter.
    /// </summary>
    public class ElementsFilter : IFilter
    {
        /// <summary>
        /// Element codenames.
        /// </summary>
        public string Elements { get; }

        /// <summary>
        /// Constructs the elements filter.
        /// </summary>
        /// <param name="elements">Codenames of elements.</param>
        public ElementsFilter(params string[] elements)
        {
            Elements = String.Join(",", elements);
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("elements={0}", Uri.EscapeDataString(Elements));
        }
    }
}