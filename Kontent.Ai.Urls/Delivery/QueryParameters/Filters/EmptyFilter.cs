namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute is empty.
    /// </summary>
    public sealed class EmptyFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        public EmptyFilter(string elementOrAttributePath) : base(elementOrAttributePath)
        {
            Operator = "[empty]";
        }
    }
}
