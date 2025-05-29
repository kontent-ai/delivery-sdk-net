namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncV2LanguageData
{
    /// <summary>
    /// Gets the system attributes of the language.
    /// </summary>
    ILanguageSystemAttributes System { get; }
}
