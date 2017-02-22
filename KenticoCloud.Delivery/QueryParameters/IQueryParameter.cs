namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a query parameter.
    /// </summary>
    public interface IQueryParameter
    {
        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        string GetQueryStringParameter();
    }
}
