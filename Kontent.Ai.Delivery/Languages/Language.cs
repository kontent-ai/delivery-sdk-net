using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(ILanguageSystemAttributes.Name) + "}")]
internal sealed record Language : ILanguage
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required LanguageSystemAttributes System { get; init; }

    ILanguageSystemAttributes ILanguage.System => System;
}
