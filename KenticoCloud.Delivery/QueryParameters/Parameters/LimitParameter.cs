using Newtonsoft.Json;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies the maximum number of content entities to return.
    /// </summary>
    public sealed class LimitParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the maximum number of content entities to return.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LimitParameter"/> class.
        /// </summary>
        /// <param name="limit">The maximum number of content entities to return.</param>
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
