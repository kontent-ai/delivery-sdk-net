namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a filter value.
/// </summary>
public interface IFilterValue
{
    /// <summary>
    /// Serializes the filter value to a string format suitable for the API.
    /// </summary>
    string Serialize();
}