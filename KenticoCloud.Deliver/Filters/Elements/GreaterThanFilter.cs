using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "greater than" operation.
    /// </summary>
    public class GreaterThanFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the GreaterThan filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public GreaterThanFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[gt]";
        }
    }
}
