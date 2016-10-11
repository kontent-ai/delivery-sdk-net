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
        public string Skip { get; }

        /// <summary>
        /// Constructs the skip filter.
        /// </summary>
        /// <param name="skip">How many content items to skip.</param>
        public SkipFilter(int skip)
        {
            Skip = skip.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the query string represention of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("skip={0}", Uri.EscapeDataString(Skip));
        }
    }
}
