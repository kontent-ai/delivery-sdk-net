namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that doesn't match a value in the specified list.
    /// </summary>
    public sealed class NotInFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotInFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="values">The filter values.</param>
        public NotInFilter(string elementOrAttributePath, params string[] values) : base(elementOrAttributePath, values)
        {
            Operator = "[nin]";
        }
    }
}
