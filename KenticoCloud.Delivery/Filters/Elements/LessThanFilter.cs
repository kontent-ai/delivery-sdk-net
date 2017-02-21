namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents "less than" operation.
    /// </summary>
    public class LessThanFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the LessThan filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public LessThanFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[lt]";
        }
    }
}