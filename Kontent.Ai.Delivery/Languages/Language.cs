using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(ILanguageSystemAttributes.Name) + "}")]
[method: JsonConstructor]
internal sealed class Language(ILanguageSystemAttributes system) : ILanguage
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public ILanguageSystemAttributes System { get; internal set; } = system;
}
