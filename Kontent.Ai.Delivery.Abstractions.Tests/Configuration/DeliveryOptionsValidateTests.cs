using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Kontent.Ai.Delivery.Abstractions.Tests.Configuration;

public class DeliveryOptionsValidateTests
{
    private static List<ValidationResult> CallValidate(DeliveryOptions options)
    {
        var context = new ValidationContext(options);
        return options.Validate(context).ToList();
    }

    [Fact]
    public void Validate_UsePreviewApiAndSecureAccess_ReturnsError()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = Guid.NewGuid().ToString(),
            UsePreviewApi = true,
            UseSecureAccess = true
        };

        var results = CallValidate(options);

        Assert.Single(results);
        Assert.Contains("Cannot use both Preview API and Secure Access simultaneously", results[0].ErrorMessage!);
        Assert.Contains(nameof(DeliveryOptions.UsePreviewApi), results[0].MemberNames);
        Assert.Contains(nameof(DeliveryOptions.UseSecureAccess), results[0].MemberNames);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_NullOrWhitespaceEnvironmentId_YieldsNoValidationResults(string? environmentId)
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = environmentId!
        };

        var results = CallValidate(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123-456")]
    [InlineData("xyz")]
    public void Validate_InvalidGuidFormat_ReturnsValidGuidError(string environmentId)
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = environmentId
        };

        var results = CallValidate(options);

        Assert.Single(results);
        Assert.Contains("must be a valid GUID", results[0].ErrorMessage!);
        Assert.Contains(nameof(DeliveryOptions.EnvironmentId), results[0].MemberNames);
    }

    [Fact]
    public void Validate_EmptyGuid_ReturnsEmptyGuidError()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = Guid.Empty.ToString()
        };

        var results = CallValidate(options);

        Assert.Single(results);
        Assert.Contains("cannot be an empty GUID", results[0].ErrorMessage!);
        Assert.Contains(nameof(DeliveryOptions.EnvironmentId), results[0].MemberNames);
    }

    [Fact]
    public void Validate_ValidGuid_ReturnsNoErrors()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = Guid.NewGuid().ToString()
        };

        var results = CallValidate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_UsePreviewApiAndSecureAccessWithWhitespaceEnvironmentId_ReturnsOnlyMutualExclusionError()
    {
        // When EnvironmentId is whitespace, the method should yield the preview+secure error
        // and then yield break (skipping the GUID checks).
        var options = new DeliveryOptions
        {
            EnvironmentId = "   ",
            UsePreviewApi = true,
            UseSecureAccess = true
        };

        var results = CallValidate(options);

        Assert.Single(results);
        Assert.Contains("Cannot use both Preview API and Secure Access simultaneously", results[0].ErrorMessage!);
    }

    [Fact]
    public void Validate_UsePreviewApiAndSecureAccessWithInvalidGuid_ReturnsBothErrors()
    {
        // When both flags are set AND the EnvironmentId is not a valid GUID,
        // both validation errors should be returned.
        var options = new DeliveryOptions
        {
            EnvironmentId = "not-a-guid",
            UsePreviewApi = true,
            UseSecureAccess = true
        };

        var results = CallValidate(options);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Cannot use both Preview API and Secure Access simultaneously"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("must be a valid GUID"));
    }

    [Fact]
    public void Validate_UsePreviewApiAndSecureAccessWithEmptyGuid_ReturnsBothErrors()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = Guid.Empty.ToString(),
            UsePreviewApi = true,
            UseSecureAccess = true
        };

        var results = CallValidate(options);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Cannot use both Preview API and Secure Access simultaneously"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("cannot be an empty GUID"));
    }
}
