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
        var property = validationContext.ObjectType.GetProperty(_propertyName) ?? throw new NotSupportedException($"Can't find {_propertyName} on searched type: {validationContext.ObjectType.Name}");
        var requiredIfTypeActualValue = property.GetValue(validationContext.ObjectInstance);

        if (requiredIfTypeActualValue == null && _isValue != null)
        {
            return ValidationResult.Success;
        }

        if (requiredIfTypeActualValue == null || requiredIfTypeActualValue.Equals(_isValue))
        {
            return value == null
                ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName))
                : ValidationResult.Success;
        }

        return ValidationResult.Success;
    }
}
