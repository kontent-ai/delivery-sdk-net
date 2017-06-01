using Newtonsoft.Json;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies the maximum level of recursion when retrieving modular content items. If not specified, the default depth is one level.
    /// </summary>
    public sealed class DepthParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the maximum level of recursion when retrieving modular content items.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthParameter"/> class using the specified depth level.
        /// </summary>
        /// <param name="depth">The maximum level of recursion to use when retrieving modular content items.</param>
        public DepthParameter(int depth)
        {
            Depth = depth;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("depth={0}", Uri.EscapeDataString(JsonConvert.ToString(Depth)));
        }
    }
}
