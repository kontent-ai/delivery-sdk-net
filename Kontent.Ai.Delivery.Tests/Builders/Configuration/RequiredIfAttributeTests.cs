using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class RequiredIfAttributeTests
{
    [Fact]
    public void RequiredIf_WhenComparedFieldMatchesAndValueMissing_ReturnsValidationError()
    {
        var model = new FieldComparedModel
        {
            IsEnabled = true,
            Value = null
        };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RequiredIf_WhenComparedFieldDoesNotMatch_DoesNotRequireValue()
    {
        var model = new FieldComparedModel
        {
            IsEnabled = false,
            Value = null
        };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    private static bool TryValidate(FieldComparedModel model, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    private sealed class FieldComparedModel
    {
        public bool IsEnabled;

        [RequiredIf(nameof(IsEnabled), true)]
        public string? Value { get; init; }
    }
}
