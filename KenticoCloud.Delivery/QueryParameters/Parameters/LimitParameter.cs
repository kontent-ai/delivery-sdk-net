using Newtonsoft.Json;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies the maximum number of content items to return.
    /// </summary>
    public sealed class LimitParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the maximum number of content items to return.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LimitParameter"/> class using the specified limit.
        /// </summary>
        /// <param name="limit">The maximum number of content items to return.</param>
        public LimitParameter(int limit)
        {
            Limit = limit;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("limit={0}", Uri.EscapeDataString(JsonConvert.ToString(Limit)));
        }
    }
}
