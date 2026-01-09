using System.Diagnostics;
using Kontent.Ai.Delivery.ContentTypes.Element;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc/>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
internal sealed record ContentType : IContentType
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required ContentTypeSystemAttributes System { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("elements")]
    public required IReadOnlyDictionary<string, ContentElement> Elements { get; init; }

    IReadOnlyDictionary<string, IContentElement> IContentType.Elements => Elements.ToDictionary(x => x.Key, x => (IContentElement)x.Value);
    IContentTypeSystemAttributes IContentType.System => System;
}