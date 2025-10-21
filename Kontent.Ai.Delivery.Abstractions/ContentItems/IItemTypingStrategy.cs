using System;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Decides the effective model type for content item deserialization.
/// </summary>
public interface IItemTypingStrategy
{
    /// <summary>
    /// Resolves the .NET model type to use for deserialization based on the content type codename.
    /// </summary>
    /// <param name="contentTypeCodename">The content type codename from Kontent.ai.</param>
    /// <returns>The .NET type that implements <see cref="IElementsModel"/> to use for deserialization.</returns>
    Type ResolveModelType(string contentTypeCodename);
}