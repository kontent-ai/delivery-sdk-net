using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Provides the base class for filter implementations.
    /// </summary>
    public abstract class Filter : IQueryParameter
    {
        /// <summary>
        /// Gets the codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.
        /// </summary>
        public string ElementOrAttributePath { get; protected set; }

        /// <summary>
        /// Gets the filter value.
        /// </summary>
        public string Value { get; protected set; }

        /// <summary>
        /// Gets the filter operator.
        /// </summary>
        public string Operator { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public Filter(string elementOrAttributePath, string value)
        {
            ElementOrAttributePath = elementOrAttributePath;
            Value = value;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("{0}{1}={2}", Uri.EscapeDataString(ElementOrAttributePath), Uri.EscapeDataString(Operator ?? string.Empty), Uri.EscapeDataString(Value));
        }
    }
}
