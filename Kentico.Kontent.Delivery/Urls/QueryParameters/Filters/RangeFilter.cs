namespace Kentico.Kontent.Delivery.Urls.QueryParameters.Filters
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that falls within the specified range of values (both inclusive).
    /// </summary>
    public sealed class RangeFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RangeFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="lowerLimit">The lower limit of the filter range.</param>
        /// <param name="upperLimit">The upper limit of the filter range.</param>
        public RangeFilter(string elementOrAttributePath, string lowerLimit, string upperLimit) : base(elementOrAttributePath, lowerLimit , upperLimit)
        {
            Operator = "[range]";
        }
    }
}
