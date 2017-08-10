namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Data holder for content items resolved by inline content items processor
    /// </summary>
    /// <typeparam name="T">Type of item being resolved</typeparam>
    public class ResolvedInlineData<T>
    {
        /// <summary>
        /// Inline data
        /// </summary>
        public T Data { get; set; }
    }
}
