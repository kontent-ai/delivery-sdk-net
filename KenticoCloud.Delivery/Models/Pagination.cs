using Newtonsoft.Json;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents information about a page.
    /// </summary>
    public sealed class Pagination
    {
        /// <summary>
        /// Gets the requested number of items to skip.
        /// </summary>
        public int Skip { get; }

        /// <summary>
        /// Gets the requested page size.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Gets the number of retrieved items.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the URL of the next page.
        /// </summary>
        /// <remarks>The URL of the next page, if available; otherwise, <see cref="string.Empty"/>.</remarks>
        public string NextPageUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pagination"/> class with information from a response.
        /// </summary>
        [JsonConstructor]
        internal Pagination(int skip, int limit, int count, string next_page)
        {
            Skip = skip;
            Limit = limit;
            Count = count;
            NextPageUrl = next_page;
        }
    }
}
