namespace KenticoKontent.Delivery
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that contains any of the specified values.
    /// This filter is applicable to array values only, such as sitemap location or value of Linked Items, Taxonomy and Multiple choice content elements.
    /// </summary>
    public sealed class AnyFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnyFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="values">The filter values.</param>
        public AnyFilter(string elementOrAttributePath, params string[] values) : base(elementOrAttributePath, values)
        {
            Operator = "[any]";
        }
    }
}
