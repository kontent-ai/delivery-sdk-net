namespace Kentico.Kontent.Delivery.Urls.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has the specified value.
    /// </summary>
    public sealed class EqualsFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EqualsFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public EqualsFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[eq]";
        }
    }
}
