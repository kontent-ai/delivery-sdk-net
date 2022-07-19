namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that contains the specified value.
    /// This filter is applicable to array values only, such as sitemap location or value of Linked Items, Taxonomy and Multiple choice content elements.
    /// </summary>
    public sealed class ContainsFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainsFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public ContainsFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[contains]";
        }
    }
}
