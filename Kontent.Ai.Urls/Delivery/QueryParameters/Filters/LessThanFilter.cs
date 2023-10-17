namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that is less than the specified value.
    /// </summary>
    public sealed class LessThanFilter<T> : Filter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessThanFilter{T}"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public LessThanFilter(string elementOrAttributePath, T value) : base(elementOrAttributePath, value)
        {
            Operator = "[lt]";
        }
    }

    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that is less than the specified value.
    /// </summary>
    public sealed class LessThanFilter : Filter<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessThanFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public LessThanFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[lt]";
        }
    }
}
