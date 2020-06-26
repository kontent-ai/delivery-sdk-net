using FluentAssertions.Equivalency;
using Kentico.Kontent.Delivery.Abstractions;
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
        public static EquivalencyAssertionOptions<T> ExcludingDateTimes<T>(this EquivalencyAssertionOptions<T> o) where T: IResponse
        {
            return o.Excluding(p => p.SelectedMemberInfo.MemberType == typeof(DateTime) || p.SelectedMemberInfo.MemberType == typeof(DateTime?));
        }
    }
}
