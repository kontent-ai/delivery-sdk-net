using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.QueryParameters.Parameters
{
    /// <summary>
    /// Specifies whether to include total count in the paging section.
    /// </summary>
    public sealed class IncludeTotalCountParameter : IQueryParameter
    {
        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return "includeTotalCount";
        }
    }
}
