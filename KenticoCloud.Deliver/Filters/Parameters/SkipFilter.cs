using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "skip" query parameter.
    /// </summary>
    public class SkipFilter : IFilter
    {
        /// <summary>
        /// How many content items to skip.
        /// </summary>
        public int Skip { get; }

        /// <summary>
        /// Constructs the skip filter.
        /// </summary>
        /// <param name="skip">How many content items to skip.</param>
        public SkipFilter(int skip)
        {
            Skip = skip;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return $"skip={Skip}";
        }
    }
}
