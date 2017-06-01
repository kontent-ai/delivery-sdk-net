using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Specifies how content items are sorted.
    /// </summary>
    public sealed class OrderParameter : IQueryParameter
    {
        /// <summary>
        /// Gets the order in which content items are sorted.
        /// </summary>
        public SortOrder SortOrder { get; }

        /// <summary>
        /// Gets the codename of a content element or system attribute by which content items are sorted.
        /// </summary>
        public string ElementOrAttributePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderParameter"/> class using the specified element or system attribute and sorting order.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute by which content items are sorted, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="sortOrder">The order in which the content items are sorted.</param>
        public OrderParameter(string elementOrAttributePath, SortOrder sortOrder = SortOrder.Ascending)
        {
            ElementOrAttributePath = elementOrAttributePath;
            SortOrder = sortOrder;
        }

        /// <summary>
        /// Returns the query string representation of the query parameter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return string.Format("order={0}{1}", Uri.EscapeDataString(ElementOrAttributePath), Uri.EscapeDataString(SortOrder == SortOrder.Ascending ? "[asc]" : "[desc]"));
        }
    }
}
