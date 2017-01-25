namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "greater than" operation.
    /// </summary>
    public class GreaterThanFilter : AbstractFilter
    {
        /// <summary>
        /// Constructs the GreaterThan filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="value">Parameter value.</param>
        public GreaterThanFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[gt]";
        }
    }
}
