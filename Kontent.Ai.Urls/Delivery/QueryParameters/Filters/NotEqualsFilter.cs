namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute doesn't have the specified value.
    /// </summary>
    public sealed class NotEqualsFilter<T> : Filter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotEqualsFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public NotEqualsFilter(string elementOrAttributePath, T value) : base(elementOrAttributePath, value)
        {
            Operator = "[neq]";
        }
    }

    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute doesn't have the specified value.
    /// </summary>
    public sealed class NotEqualsFilter : Filter<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotEqualsFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public NotEqualsFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[neq]";
        }
    }
}
