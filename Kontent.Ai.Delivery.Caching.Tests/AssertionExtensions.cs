using FluentAssertions;
using FluentAssertions.Equivalency;
using System;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public static class AssertionExtensions
    {
        /// <summary>
        /// Excludes <see cref="DateTime"/> and its nullable counterpart from comparison. Useful for comparing deserialized BSON objects which lost their date/time precision during serialization.
        /// </summary>
        /// <typeparam name="T">Type of object that the exceptions is applied to.</typeparam>
        /// <param name="o">Type-specific behavior options.</param>
        public static EquivalencyAssertionOptions<T> DateTimesBsonCorrection<T>(this EquivalencyAssertionOptions<T> o)
        {
            return ApproximateDateTimes(o, 1);
        }

        /// <summary>
        /// Excludes <see cref="DateTime"/> and its nullable counterpart from comparison. Useful for comparing deserialized BSON objects which lost their date/time precision during serialization.
        /// </summary>
        /// <typeparam name="T">Type of object that the exceptions is applied to.</typeparam>
        /// <param name="o">Type-specific behavior options.</param>
        /// <param name="precision">The maximum amount of milliseconds which the two values may differ.</param>
        public static EquivalencyAssertionOptions<T> ApproximateDateTimes<T>(this EquivalencyAssertionOptions<T> o, int precision) 
        {
            return o.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision)).WhenTypeIs<DateTime>()
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision)).WhenTypeIs<DateTime?>();
        }
    }
}
