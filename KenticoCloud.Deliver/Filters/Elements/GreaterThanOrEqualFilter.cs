using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "greater than or equal" operation.
    /// </summary>
    public class GreaterThanOrEqualFilter : BaseFilter, IElementsFilter
    {
        /// <summary>
        /// Constructs the GreaterThanOrEqual filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public GreaterThanOrEqualFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[gte]";
        }
    }
}
