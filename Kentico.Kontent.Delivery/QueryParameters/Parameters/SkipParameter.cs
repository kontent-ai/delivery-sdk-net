using System;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.QueryParameters.Parameters
{
    /// <summary>
    /// Specifies the number of content items to skip.
    /// </summary>
    public sealed class SkipParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the number of content items to skip.
        /// </summary>
        public int Skip { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipParameter"/> class using the specified number of items to skip.
        /// </summary>
        /// <param name="skip">The number of content items to skip.</param>
        public SkipParameter(int skip)
        {
            Skip = skip;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return $"skip={Uri.EscapeDataString(JsonConvert.ToString(Skip))}";
        }
    }
}
