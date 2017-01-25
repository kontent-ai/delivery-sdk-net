namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents a query parameter filter.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        string GetQueryStringParameter();
    }
}
