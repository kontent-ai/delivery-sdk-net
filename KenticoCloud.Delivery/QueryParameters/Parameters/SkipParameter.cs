using Newtonsoft.Json;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies the number of content entities to skip.
    /// </summary>
    public sealed class SkipParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the number of content entities to skip.
        /// </summary>
        public int Skip { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipParameter"/> class.
        /// </summary>
        /// <param name="skip">The number of content entities to skip.</param>
        public SkipParameter(int skip)
        {
            Skip = skip;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("skip={0}", Uri.EscapeDataString(JsonConvert.ToString(Skip)));
        }
    }
}
