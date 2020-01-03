namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a filter that matches a content item if the specified content element or system attribute has a value that matches a value in the specified list.
    /// </summary>
    public sealed class InFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InFilter"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="values">The filter values.</param>
        public InFilter(string elementOrAttributePath, params string[] values) : base(elementOrAttributePath, values)
        {
            Operator = "[in]";
        }
    }
}
