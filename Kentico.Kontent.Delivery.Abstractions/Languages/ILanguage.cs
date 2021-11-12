namespace Kentico.Kontent.Delivery.Abstractions.Languages
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
