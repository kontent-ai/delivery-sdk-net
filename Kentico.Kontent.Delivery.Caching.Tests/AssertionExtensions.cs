using FluentAssertions;
using FluentAssertions.Equivalency;
using System;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public static class AssertionExtensions
    {
        /// <summary>
        /// Excludes <see cref="DateTime"/> and <see cref="DateTime?"/> from comparison. Useful for comparing deserialized BSON objects which lost their date/time precision during serialization.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static EquivalencyAssertionOptions<T> DateTimesBsonCorrection<T>(this EquivalencyAssertionOptions<T> o)
        {
            return ApproximateDateTimes(o, 1);
        }

        /// <summary>
        /// Excludes <see cref="DateTime"/> and <see cref="DateTime?"/> from comparison. Useful for comparing deserialized BSON objects which lost their date/time precision during serialization.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static EquivalencyAssertionOptions<T> ApproximateDateTimes<T>(this EquivalencyAssertionOptions<T> o, int precision) 
        {
            return o.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision)).WhenTypeIs<DateTime>()
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, precision)).WhenTypeIs<DateTime?>();
        }
    }
}
