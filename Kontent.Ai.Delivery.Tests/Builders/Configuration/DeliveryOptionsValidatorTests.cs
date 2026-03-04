using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class DeliveryOptionsValidatorTests
{
    private readonly Guid _guid = Guid.NewGuid();

    private static bool TryValidate(DeliveryOptions options, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(options);
        return Validator.TryValidateObject(options, context, results, validateAllProperties: true);
    }

    [Fact]
    public void ValidateOptions_WithEmptyEnvironmentId_Fails()
    {
        var options = new DeliveryOptions { EnvironmentId = string.Empty };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void ValidateOptions_WithNullEnvironmentId_Fails()
    {
        var options = new DeliveryOptions { EnvironmentId = null! };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("123-456")]
    public void ValidateOptions_WithInvalidEnvironmentIdFormat_Fails(string environmentId)
    {
        var options = new DeliveryOptions { EnvironmentId = environmentId };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void ValidateOptions_WithUppercaseEnvironmentId_Passes()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString().ToUpperInvariant()
        };

        var isValid = TryValidate(options, out var results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void ValidateOptions_UseOfPreviewAndSecureAccessSimultaneously_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UsePreviewApi = true,
            PreviewApiKey = "abc.def.ghi",
            UseSecureAccess = true,
            SecureAccessApiKey = "jkl.mno.pqr"
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("Cannot use both Preview API and Secure Access simultaneously."));
    }

    [Fact]
    public void ValidateOptions_PreviewEnabledWithoutKey_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UsePreviewApi = true,
            PreviewApiKey = null
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("PreviewApiKey is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOptions_PreviewEnabledWithEmptyOrWhitespaceKey_Fails(string previewKey)
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UsePreviewApi = true,
            PreviewApiKey = previewKey
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("PreviewApiKey is required"));
    }

    [Fact]
    public void ValidateOptions_SecureAccessEnabledWithoutKey_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UseSecureAccess = true,
            SecureAccessApiKey = null
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("SecureAccessApiKey is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOptions_SecureAccessEnabledWithEmptyOrWhitespaceKey_Fails(string secureKey)
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UseSecureAccess = true,
            SecureAccessApiKey = secureKey
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("SecureAccessApiKey is required"));
    }

    [Fact]
    public void ValidateOptions_PreviewKeyWithInvalidFormat_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UsePreviewApi = true,
            PreviewApiKey = "badPreviewApiFormat"
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("The Preview API key must be a valid API key."));
    }

    [Fact]
    public void ValidateOptions_SecureAccessKeyWithInvalidFormat_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UseSecureAccess = true,
            SecureAccessApiKey = "invalid"
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("The Secure Access API key must be a valid API key."));
    }

    [Fact]
    public void ValidateOptions_ProductionEndpointWithInvalidFormat_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            ProductionEndpoint = "invalid"
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DeliveryOptions.ProductionEndpoint)));
    }

    [Fact]
    public void ValidateOptions_PreviewEndpointWithInvalidFormat_Fails()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            PreviewEndpoint = "invalid"
        };

        var isValid = TryValidate(options, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DeliveryOptions.PreviewEndpoint)));
    }

    [Fact]
    public void Validate_WithWhitespaceEnvironmentId_YieldsNoCustomGuidErrors()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = "   "
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithEmptyGuidEnvironmentId_YieldsEmptyGuidError()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = Guid.Empty.ToString()
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.Contains(results, r => r.ErrorMessage == "EnvironmentId cannot be an empty GUID.");
    }
}
