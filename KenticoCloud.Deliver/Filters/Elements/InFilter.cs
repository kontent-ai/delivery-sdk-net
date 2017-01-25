namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "in" operator.
    /// </summary>
    public class InFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the In filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public InFilter(string element, params string[] value)
            : base(element, string.Join(",", value))
        {
            Operator = "[in]";
        }
    }
}
