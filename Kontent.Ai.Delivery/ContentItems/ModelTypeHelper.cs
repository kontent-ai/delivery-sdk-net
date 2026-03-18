namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Shared utility for checking whether a model type represents dynamic (untyped) content.
/// </summary>
internal static class ModelTypeHelper
{
    /// <summary>
    /// Returns true if <typeparamref name="TModel"/> is a dynamic content type
    /// (<see cref="IDynamicElements"/> or <see cref="DynamicElements"/>).
    /// </summary>
    public static bool IsDynamic<TModel>() => IsDynamic(typeof(TModel));

    /// <summary>
    /// Returns true if <paramref name="modelType"/> is a dynamic content type
    /// (<see cref="IDynamicElements"/> or <see cref="DynamicElements"/>).
    /// </summary>
    public static bool IsDynamic(Type modelType) =>
        modelType == typeof(IDynamicElements) || modelType == typeof(DynamicElements);
}
