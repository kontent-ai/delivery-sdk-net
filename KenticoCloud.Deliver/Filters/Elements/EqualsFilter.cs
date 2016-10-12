using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "equals" operation.
    /// </summary>
    public class EqualsFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the Equals filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public EqualsFilter(string element, string value)
            : base (element, value)
        {
        }
    }
}
