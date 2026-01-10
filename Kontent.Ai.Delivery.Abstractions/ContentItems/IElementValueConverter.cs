namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Provides value conversion for content element values without reflection dependency.
/// </summary>
/// <typeparam name="T">The source element value type</typeparam>
/// <typeparam name="TResult">The converted result type</typeparam>
public interface IElementValueConverter<in T, TResult>
{
    /// <summary>
    /// Converts an element value to the target result type.
    /// </summary>
    /// <typeparam name="TElement">The element type containing the value</typeparam>
    /// <param name="element">Source element data</param>
    /// <param name="context">Context of the current resolving process</param>
    /// <returns>The converted value, or null if conversion is not applicable</returns>
    Task<TResult?> ConvertAsync<TElement>(TElement element, ResolvingContext context) where TElement : IContentElementValue<T>;
}