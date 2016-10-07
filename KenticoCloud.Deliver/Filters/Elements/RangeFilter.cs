using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "range" operation.
    /// </summary>
    public class RangeFilter : BaseFilter, IElementsFilter
    {
        /// <summary>
        /// Constructs the Range filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public RangeFilter(string element, string lowerEndpoint, string upperEndpoint)
            : base(element, lowerEndpoint + "," + upperEndpoint)
        {
            Operator = "[range]";
        }
    }
}
