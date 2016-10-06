using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "in" operation.
    /// </summary>
    public class InFilter : BaseFilter, IElementsFilter
    {
        /// <summary>
        /// Constructs the In filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public InFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[in]";
        }
    }
}
