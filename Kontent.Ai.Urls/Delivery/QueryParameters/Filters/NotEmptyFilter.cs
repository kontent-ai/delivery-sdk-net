namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute is not empty.
    /// </summary>
    public sealed class NotEmptyFilter : Filter<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotEmptyFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        public NotEmptyFilter(string elementOrAttributePath) : base(elementOrAttributePath)
        {
            Operator = "[nempty]";
        }
    }
}
