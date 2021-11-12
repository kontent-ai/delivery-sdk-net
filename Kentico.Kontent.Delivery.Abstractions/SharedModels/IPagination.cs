namespace Kentico.Kontent.Delivery.Abstractions.SharedModels
{
    /// <summary>
    /// Represents information about a page.
    /// </summary>
    public interface IPagination
    {
        /// <summary>
        /// Gets the number of retrieved items.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets the requested page size.
        /// </summary>
        int Limit { get; }

        /// <summary>
        /// Gets the URL of the next page.
        /// </summary>
        /// <remarks>The URL of the next page, if available; otherwise, <see cref="string.Empty"/>.</remarks>
        string NextPageUrl { get; }

        /// <summary>
        /// Gets the requested number of items to skip.
        /// </summary>
        int Skip { get; }
        /// <summary>
        /// Gets the total number of items matching the search criteria.
        /// </summary>
        int? TotalCount { get; }
    }
}