namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Data holder for content items resolved by inline content items processor
    /// </summary>
    /// <typeparam name="T">Type of item being resolved</typeparam>
    public class ResolvedContentItemData<T>
    {
        /// <summary>
        /// Content item
        /// </summary>
        public T Item { get; set; }
    }
}