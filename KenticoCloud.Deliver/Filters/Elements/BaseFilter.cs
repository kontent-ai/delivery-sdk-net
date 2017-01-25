using System;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Base class for the elements filters.
    /// </summary>
    public abstract class AbstractFilter : IFilter

    {
        /// <summary>
        /// Element codename.
        /// </summary>
        public string Element { get; protected set; }

        /// <summary>
        /// Parameter value.
        /// </summary>
        public string Value { get; protected set; }

        /// <summary>
        /// Query operator.
        /// </summary>
        public string Operator { get; protected set; }

        /// <summary>
        /// Abstract constructor.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public AbstractFilter(string element, string value)
        {
            Element = element;
            Value = value;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("{0}{1}={2}", Uri.EscapeDataString(Element), Operator, Uri.EscapeDataString(Value));
        }
    }
}
