namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a language.
    /// </summary>
    public interface ILanguage
    {
        /// <summary>
        /// Gets the system attributes of the language.
        /// </summary>
        ILanguageSystemAttributes System { get; }
    }
}
