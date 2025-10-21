using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IEmbeddedContent" />
[DebuggerDisplay("Type = {ContentTypeCodename}, Codename = {Codename}")]
internal record EmbeddedContent(
    string ContentTypeCodename,
    string Codename,
    string? Name,
    Guid Id,
    object? Elements
) : IEmbeddedContent
{
    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public EmbeddedContent() : this(string.Empty, string.Empty, null, Guid.Empty, null)
    {
    }
}