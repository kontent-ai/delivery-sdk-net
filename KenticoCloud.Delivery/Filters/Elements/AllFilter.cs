namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents "all" operator.
    /// </summary>
    public class AllFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the All filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public AllFilter(string element, params string[] value)
            : base(element, string.Join(",", value))
        {
            Operator = "[all]";
        }
    }
}