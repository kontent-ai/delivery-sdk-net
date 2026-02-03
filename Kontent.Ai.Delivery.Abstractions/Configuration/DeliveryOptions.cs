using System.ComponentModel.DataAnnotations;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents configuration of the <see cref="IDeliveryClient"/>.
/// </summary>
public sealed class DeliveryOptions : IValidatableObject
{
    /// <summary>
    /// Gets or sets the environment ID.
    /// </summary>
    [Required]
    [RegularExpression(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", ErrorMessage = "The environment ID must be a valid GUID.")]
    public string EnvironmentId { get; set; } = Guid.Empty.ToString();

    /// <summary>
    /// Gets or sets a value that determines if the client uses resilience policies.
    /// </summary>
    public bool EnableResilience { get; set; } = true;

    /// <summary>
    /// Gets or sets the format of the Production API endpoint address.
    /// </summary>
    [Url]
    public string ProductionEndpoint { get; set; } = "https://deliver.kontent.ai";

    /// <summary>
    /// Gets or sets the format of the Preview API endpoint address.
    /// </summary>
    [Url]
    public string PreviewEndpoint { get; set; } = "https://preview-deliver.kontent.ai";

    /// <summary>
    /// Gets or sets the API key that is used to retrieve content with the Preview API.
    /// </summary>
    [RequiredIf(nameof(UsePreviewApi), true, ErrorMessage = "PreviewApiKey is required when using the Preview API.")]
    [RegularExpression(@"[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+", ErrorMessage = "The Preview API key must be a valid API key.")]
    public string? PreviewApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the Preview API is used to retrieve content.
    /// If the Preview API is used the <see cref="PreviewApiKey"/> must be set.
    /// </summary>
    public bool UsePreviewApi { get; set; } = false;

    /// <summary>
    /// Gets or sets a value that determines if the client sends the secure access API key to retrieve content with the Production API.
    /// This key is required to retrieve content when secure access is enabled.
    /// To retrieve content when secure access is enabled the <see cref="SecureAccessApiKey"/> must be set.
    /// </summary>
    public bool UseSecureAccess { get; set; } = false;

    /// <summary>
    /// Gets or sets the API key that is used to retrieve content with the Production API when secure access is enabled.
    /// </summary>
    [RequiredIf(nameof(UseSecureAccess), true, ErrorMessage = "SecureAccessApiKey is required when using the Production API with secure access.")]
    [RegularExpression(@"[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+", ErrorMessage = "The Secure Access API key must be a valid API key.")]
    public string? SecureAccessApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the client includes the total number of items matching the search criteria in response.
    /// This behavior can also be enabled for individual requests with the IncludeTotalCountParameter.
    /// </summary>
    public bool IncludeTotalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the SDK should wait for the newest published content to be fully loaded
    /// before returning a response. When enabled, requests include the
    /// <c>X-KC-Wait-For-Loading-New-Content</c> header. This option can be overridden per query.
    /// Default is <c>false</c>.
    /// </summary>
    public bool WaitForLoadingNewContent { get; set; } = false;

    /// <summary>
    /// Gets or sets a value of codename for the rendition preset to be applied by default to the base asset URL path.
    /// If no value is specified, asset URLs will always point to non-customized variant of the image.
    /// </summary>
    public string? DefaultRenditionPreset { get; set; }

    /// <summary>
    /// Validates cross-field constraints for delivery options.
    /// Ensures mutual exclusivity of <see cref="UsePreviewApi"/> and <see cref="UseSecureAccess"/>.
    /// Validates that <see cref="EnvironmentId"/> is not an empty GUID.
    /// Uses yield semantics so other attribute-based validations also execute.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UsePreviewApi && UseSecureAccess)
        {
            yield return new ValidationResult(
                "Cannot use both Preview API and Secure Access simultaneously.",
                [nameof(UsePreviewApi), nameof(UseSecureAccess)]);
        }

        if (Guid.TryParse(EnvironmentId, out var environmentGuid) && environmentGuid == Guid.Empty)
        {
            yield return new ValidationResult(
                "EnvironmentId cannot be an empty GUID.",
                [nameof(EnvironmentId)]);
        }
    }
}
