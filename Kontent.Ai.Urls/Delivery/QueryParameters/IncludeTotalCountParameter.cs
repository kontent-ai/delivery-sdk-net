﻿using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters
{
    /// <summary>
    /// Specifies whether to include total count in the paging section.
    /// This behavior can also be enabled globally via the <see cref="DeliveryOptions.IncludeTotalCount"/>.
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
