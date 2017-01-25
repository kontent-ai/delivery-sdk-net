namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "greater than or equal" operation.
    /// </summary>
    public class GreaterThanOrEqualFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the GreaterThanOrEqual filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public GreaterThanOrEqualFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[gte]";
        }
    }
}
