using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "depth" query parameter.
    /// </summary>
    public class DepthFilter : IFilter
    {
        /// <summary>
        /// Depth.
        /// </summary>
        public string Depth { get; }

        /// <summary>
        /// Constructs the depth filter.
        /// </summary>
        /// <param name="depth">Depth.</param>
        public DepthFilter(int depth)
        {
            Depth = depth.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the query string represention of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("depth={0}", Uri.EscapeDataString(Depth));
        }
    }
}
