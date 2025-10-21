namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default type provider that returns null for all mappings,
/// enabling fallback to dynamic types via the typing strategy.
/// </summary>
/// <remarks>
/// This class is intended to be replaced by generated type providers
/// from the Kontent.ai model generator tool, which creates a dictionary
/// mapping content type codenames to their corresponding CLR types.
/// By returning null, the default implementation instructs
/// <see cref="DefaultItemTypingStrategy"/> to use dynamic types.
/// </remarks>
internal class TypeProvider : ITypeProvider
{
    public Type? TryGetModelType(string contentType)
        => null;

    public string? GetCodename(Type contentType)
        => null;
}