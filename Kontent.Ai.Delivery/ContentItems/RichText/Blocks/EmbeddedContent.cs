using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IEmbeddedContent" />
[DebuggerDisplay("Type = {ContentTypeCodename}, Codename = {Codename}")]
internal record EmbeddedContent(
    string ContentTypeCodename,
    string Codename,
    string? Name,
    Guid Id,
    object? Elements
) : IEmbeddedContent;