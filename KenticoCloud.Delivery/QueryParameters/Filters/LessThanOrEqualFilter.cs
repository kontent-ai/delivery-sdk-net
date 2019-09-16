namespace KenticoKontent.Delivery
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that is less than or equal to the specified value.
    /// </summary>
    public sealed class LessThanOrEqualFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LessThanOrEqualFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="value">The filter value.</param>
        public LessThanOrEqualFilter(string elementOrAttributePath, string value) : base(elementOrAttributePath, value)
        {
            Operator = "[lte]";
        }
    }
}
