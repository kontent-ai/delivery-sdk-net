namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that is greater than or equal to the specified value.
    /// </summary>
    public sealed class GreaterThanOrEqualFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GreaterThanOrEqualFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public GreaterThanOrEqualFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[gte]";
        }
    }
}
