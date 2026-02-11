using System.ComponentModel.DataAnnotations;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// A custom validation attribute that checks if a property is required based on the value of another property.
/// </summary>
/// <param name="propertyName">The name of the property that is used to check if the current property is required.</param>
/// <param name="isValue">The value of the property that is used to check if the current property is required.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class RequiredIfAttribute(string propertyName, object? isValue) : ValidationAttribute
{
    private readonly string _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    private readonly object? _isValue = isValue;

    /// <summary>
    /// Formats the error message for the validation attribute.
    /// </summary>
    /// <param name="name">The name of the property that is used to check if the current property is required.</param>
    /// <returns>The formatted error message.</returns>
    public override string FormatErrorMessage(string name)
    {
        var errorMessage = $"Property {name} is required when {_propertyName} is {_isValue}";
        return ErrorMessage ?? errorMessage;
    }

    /// <summary>
    /// Checks if the current property is required based on the value of another property.
    /// </summary>
    /// <param name="value">The value of the current property.</param>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation result.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        var requiredIfTypeActualValue = GetComparedMemberValue(validationContext);
        var isRequired = Equals(requiredIfTypeActualValue, _isValue);

        if (!isRequired)
        {
            return ValidationResult.Success;
        }

        return IsMissingValue(value)
            ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName))
            : ValidationResult.Success;
    }

    private static bool IsMissingValue(object? value) =>
        value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            _ => false
        };

    private object? GetComparedMemberValue(ValidationContext validationContext)
    {
        var comparedProperty = validationContext.ObjectType.GetProperty(_propertyName);
        if (comparedProperty is not null)
        {
            return comparedProperty.GetValue(validationContext.ObjectInstance);
        }

        var comparedField = validationContext.ObjectType.GetField(_propertyName);

        return comparedField is not null
            ? comparedField.GetValue(validationContext.ObjectInstance)
            : throw new NotSupportedException($"Can't find {_propertyName} on searched type: {validationContext.ObjectType.Name}");
    }
}
