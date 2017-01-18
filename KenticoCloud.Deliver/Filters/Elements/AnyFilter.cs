using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "any" operator.
    /// </summary>
    public class AnyFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the Any filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public AnyFilter(string element, params string[] value)
            : base(element, string.Join(",", value))
        {
            Operator = "[any]";
        }
    }
}
