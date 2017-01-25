namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "less than or equal" operation.
    /// </summary>
    public class LessThanOrEqualFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the LessThanOrEqual filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public LessThanOrEqualFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[lte]";
        }
    }
}
