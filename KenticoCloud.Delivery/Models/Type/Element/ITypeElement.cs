namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents content type element.
    /// </summary>
    public interface ITypeElement
    {
        /// <summary>
        /// Element's type.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Element's name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Element's codename
        /// </summary>
        string Codename { get; }
    }
}