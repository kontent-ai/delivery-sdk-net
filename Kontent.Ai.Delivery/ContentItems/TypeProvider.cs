namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default type provider that returns null for all mappings,
/// enabling fallback to dynamic types via the typing strategy.
/// </summary>
internal class TypeProvider : ITypeProvider
{
    public Type? TryGetModelType(string contentType)
        => null;

    public string? GetCodename(Type contentType)
        => null;
}
