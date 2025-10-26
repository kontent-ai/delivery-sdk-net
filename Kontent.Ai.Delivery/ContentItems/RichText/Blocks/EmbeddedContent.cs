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

/// <inheritdoc cref="IEmbeddedContent{TModel}" />
[DebuggerDisplay("Type = {ContentTypeCodename}, Codename = {Codename}, Model = {typeof(TModel).Name}")]
internal record EmbeddedContent<TModel>(
    string ContentTypeCodename,
    string Codename,
    string? Name,
    Guid Id,
    TModel Elements
) : IEmbeddedContent<TModel>
    where TModel : IElementsModel
{
    /// <summary>
    /// Explicit interface implementation for non-generic access.
    /// </summary>
    object? IEmbeddedContent.Elements => Elements;
}