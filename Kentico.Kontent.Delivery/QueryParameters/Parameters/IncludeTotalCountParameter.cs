using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Specifies whether to include total count in the paging section.
    /// </summary>
    public sealed class IncludeTotalCountParameter : IQueryParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeTotalCountParameter"/> class.
        /// </summary>
        public IncludeTotalCountParameter()
        {
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return "includeTotalCount";
        }
    }
}
