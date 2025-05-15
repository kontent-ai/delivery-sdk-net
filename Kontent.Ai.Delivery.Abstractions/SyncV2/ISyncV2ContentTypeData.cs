namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncV2ContentTypeData
{
    /// <summary>
    /// Gets the system attributes of the content type.
    /// </summary>
    public IContentTypeSystemAttributes System { get; }
}
