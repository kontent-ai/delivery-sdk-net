namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for selecting language variant of returned items.
/// </summary>
public interface ILanguageParam
{
    /// <summary>
    /// Determines which language variant of content items to return. If not specified, the default language is used.
    /// </summary>
    public string? Language { get; set; }
}