using System;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters
{
    /// <summary>
    /// Specifies the language of content items to be requested.
    /// </summary>
    public sealed class LanguageParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the language of content items to be requested.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageParameter"/> class using the specified language.
        /// </summary>
        /// <param name="language">Language of content items to be requested.</param>
        public LanguageParameter(string language)
        {
            Language = language;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return $"language={Uri.EscapeDataString(Language)}";
        }
    }
}
