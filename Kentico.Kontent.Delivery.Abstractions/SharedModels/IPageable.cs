namespace Kentico.Kontent.Delivery.Abstractions.SharedModels
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
