using System;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Provides the base class for filter implementations.
    /// </summary>
    public abstract class Filter : IQueryParameter
    {
        private static string SEPARATOR = Uri.EscapeDataString(",");

        /// <summary>
        /// Gets the codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.
        /// </summary>
        public string ElementOrAttributePath { get; protected set; }

        /// <summary>
        /// Gets the filter values.
        /// </summary>
        public string[] Values { get; protected set; }

        /// <summary>
        /// Gets the filter operator.
        /// </summary>
        public string Operator { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="values">The filter values.</param>
        public Filter(string elementOrAttributePath, params string[] values)
        {
            ElementOrAttributePath = elementOrAttributePath;
            Values = values;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            var escapedValues = Values.Select(Uri.EscapeDataString);
            return string.Format("{0}{1}={2}", Uri.EscapeDataString(ElementOrAttributePath), Uri.EscapeDataString(Operator ?? string.Empty), string.Join(SEPARATOR, escapedValues));
        }
    }
}
