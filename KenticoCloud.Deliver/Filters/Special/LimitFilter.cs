using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "limit" query parameter.
    /// </summary>
    public class LimitFilter : IFilter
    {
        /// <summary>
        /// Maximal number of content items.
        /// </summary>
        public string Limit { get; }

        /// <summary>
        /// Constructs the limit filter.
        /// </summary>
        /// <param name="limit">Maximal number of content items.</param>
        public LimitFilter(int limit)
        {
            Limit = limit.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the query string represention of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("limit={0}", Uri.EscapeDataString(Limit));
        }
    }
}
