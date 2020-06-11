namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a pageable response.
    /// </summary>
    public interface IPageable
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        IPagination Pagination { get; }
    }
}
