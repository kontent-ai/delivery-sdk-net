using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Abstractions;

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

    [Fact]
    public void RequiredIf_ConstructorThrowsWhenPropertyNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RequiredIfAttribute(null!, true));
    }

    [Fact]
    public void FormatErrorMessage_ReturnsCustomErrorMessageWhenSet()
    {
        var attribute = new RequiredIfAttribute("SomeProperty", true)
        {
            ErrorMessage = "Custom error message"
        };

        var result = attribute.FormatErrorMessage("TestField");

        Assert.Equal("Custom error message", result);
    }

    [Fact]
    public void FormatErrorMessage_ReturnsDefaultMessageWhenErrorMessageNotSet()
    {
        var attribute = new RequiredIfAttribute("SomeProperty", true);

        var result = attribute.FormatErrorMessage("TestField");

        Assert.Contains("TestField", result);
        Assert.Contains("SomeProperty", result);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequiredIf_WhenConditionMatchesAndValueIsNonNullNonString_IsValid()
    {
        var model = new NonStringValueModel
        {
            IsEnabled = true,
            Count = 42
        };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void RequiredIf_WhenConditionMatchesAndValueIsEmptyString_ReturnsError()
    {
        var model = new FieldComparedModel
        {
            IsEnabled = true,
            Value = "   "
        };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RequiredIf_WhenComparedMemberDoesNotExist_ThrowsNotSupportedException()
    {
        var model = new MissingMemberModel
        {
            Value = null
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        Assert.Throws<NotSupportedException>(() =>
            Validator.TryValidateObject(model, context, results, validateAllProperties: true));
    }

    [Fact]
    public void RequiredIf_WhenConditionMatchesViaProperty_ReturnsValidationError()
    {
        var model = new PropertyComparedModel
        {
            IsEnabled = true,
            Value = null
        };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage is not null && r.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryValidate(object model, out List<ValidationResult> results)
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

    private sealed class NonStringValueModel
    {
        public bool IsEnabled;

        [RequiredIf(nameof(IsEnabled), true)]
        public int? Count { get; init; }
    }

    private sealed class MissingMemberModel
    {
        [RequiredIf("NonExistentMember", true)]
        public string? Value { get; init; }
    }

    private sealed class PropertyComparedModel
    {
        public bool IsEnabled { get; init; }

        [RequiredIf(nameof(IsEnabled), true)]
        public string? Value { get; init; }
    }
}
