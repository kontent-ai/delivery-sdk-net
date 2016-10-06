using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Base class for the elements filters.
    /// </summary>
    public class BaseFilter : IElementsFilter

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
        /// 
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public BaseFilter(string element, string value)
        {
            Element = element;
            Value = value;
        }

        /// <summary>
        /// Returns the query string represention of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("{0}{1}={2}", Uri.EscapeDataString(Element), Operator, Uri.EscapeDataString(Value));
        }
    }
}
