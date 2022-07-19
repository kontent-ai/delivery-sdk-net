namespace Kontent.Ai.Delivery.Abstractions
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
