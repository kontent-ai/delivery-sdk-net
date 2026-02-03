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
    /// <returns>The .NET type to use for deserialization (any POCO or <see cref="IDynamicElements"/>).</returns>
    Type ResolveModelType(string contentTypeCodename);
}
