namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents "depth" query parameter.
    /// </summary>
    public class DepthFilter : IFilter
    {
        /// <summary>
        /// Depth.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Constructs the depth filter.
        /// </summary>
        /// <param name="depth">Depth.</param>
        public DepthFilter(int depth)
        {
            Depth = depth;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return $"depth={Depth}";
        }
    }
}
