using System.ComponentModel.DataAnnotations;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class PositiveTimeSpanAttributeTests
{
    [Fact]
    public void PositiveTimeSpan_WhenValueIsPositive_IsValid()
    {
        var model = new PositiveTimeSpanModel { Duration = TimeSpan.FromMinutes(5) };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void PositiveTimeSpan_WhenValueIsZero_ReturnsError()
    {
        var model = new PositiveTimeSpanModel { Duration = TimeSpan.Zero };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("greater than TimeSpan.Zero", results[0].ErrorMessage);
    }

    [Fact]
    public void PositiveTimeSpan_WhenValueIsNegative_ReturnsError()
    {
        var model = new PositiveTimeSpanModel { Duration = TimeSpan.FromMinutes(-1) };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
    }

    [Fact]
    public void PositiveTimeSpan_WhenValueIsNull_IsValid()
    {
        var model = new NullablePositiveTimeSpanModel { Duration = null };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void PositiveTimeSpan_WhenValueIsNotTimeSpan_ReturnsError()
    {
        var model = new WrongTypeModel { Duration = "not a timespan" };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("must be a TimeSpan value", results[0].ErrorMessage);
    }

    [Fact]
    public void PositiveTimeSpan_UsesCustomErrorMessageWhenSet()
    {
        var model = new CustomErrorPositiveModel { Duration = TimeSpan.Zero };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Equal("Custom positive error", results[0].ErrorMessage);
    }

    [Fact]
    public void PositiveTimeSpan_UsesDefaultErrorMessageWhenNotSet()
    {
        var model = new DefaultErrorPositiveModel { Duration = TimeSpan.Zero };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("must be greater than TimeSpan.Zero", results[0].ErrorMessage);
    }

    private static bool TryValidate(object model, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    private sealed class PositiveTimeSpanModel
    {
        [PositiveTimeSpan(ErrorMessage = "Duration must be greater than TimeSpan.Zero.")]
        public TimeSpan Duration { get; set; }
    }

    private sealed class NullablePositiveTimeSpanModel
    {
        [PositiveTimeSpan]
        public TimeSpan? Duration { get; set; }
    }

    private sealed class WrongTypeModel
    {
        [PositiveTimeSpan]
        public object? Duration { get; set; }
    }

    private sealed class CustomErrorPositiveModel
    {
        [PositiveTimeSpan(ErrorMessage = "Custom positive error")]
        public TimeSpan Duration { get; set; }
    }

    private sealed class DefaultErrorPositiveModel
    {
        [PositiveTimeSpan]
        public TimeSpan Duration { get; set; }
    }
}

public class NonNegativeTimeSpanAttributeTests
{
    [Fact]
    public void NonNegativeTimeSpan_WhenValueIsPositive_IsValid()
    {
        var model = new NonNegativeTimeSpanModel { Duration = TimeSpan.FromMinutes(5) };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void NonNegativeTimeSpan_WhenValueIsZero_IsValid()
    {
        var model = new NonNegativeTimeSpanModel { Duration = TimeSpan.Zero };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void NonNegativeTimeSpan_WhenValueIsNegative_ReturnsError()
    {
        var model = new NonNegativeTimeSpanModel { Duration = TimeSpan.FromMinutes(-1) };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("cannot be negative", results[0].ErrorMessage);
    }

    [Fact]
    public void NonNegativeTimeSpan_WhenValueIsNull_IsValid()
    {
        var model = new NullableNonNegativeTimeSpanModel { Duration = null };

        var isValid = TryValidate(model, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void NonNegativeTimeSpan_WhenValueIsNotTimeSpan_ReturnsError()
    {
        var model = new WrongTypeModel { Duration = 12345 };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("must be a TimeSpan value", results[0].ErrorMessage);
    }

    [Fact]
    public void NonNegativeTimeSpan_UsesCustomErrorMessageWhenSet()
    {
        var model = new CustomErrorNonNegativeModel { Duration = TimeSpan.FromSeconds(-1) };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Equal("Custom non-negative error", results[0].ErrorMessage);
    }

    [Fact]
    public void NonNegativeTimeSpan_UsesDefaultErrorMessageWhenNotSet()
    {
        var model = new DefaultErrorNonNegativeModel { Duration = TimeSpan.FromSeconds(-1) };

        var isValid = TryValidate(model, out var results);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("cannot be negative", results[0].ErrorMessage);
    }

    private static bool TryValidate(object model, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    private sealed class NonNegativeTimeSpanModel
    {
        [NonNegativeTimeSpan(ErrorMessage = "Duration cannot be negative.")]
        public TimeSpan Duration { get; set; }
    }

    private sealed class NullableNonNegativeTimeSpanModel
    {
        [NonNegativeTimeSpan]
        public TimeSpan? Duration { get; set; }
    }

    private sealed class WrongTypeModel
    {
        [NonNegativeTimeSpan]
        public object? Duration { get; set; }
    }

    private sealed class CustomErrorNonNegativeModel
    {
        [NonNegativeTimeSpan(ErrorMessage = "Custom non-negative error")]
        public TimeSpan Duration { get; set; }
    }

    private sealed class DefaultErrorNonNegativeModel
    {
        [NonNegativeTimeSpan]
        public TimeSpan Duration { get; set; }
    }
}
