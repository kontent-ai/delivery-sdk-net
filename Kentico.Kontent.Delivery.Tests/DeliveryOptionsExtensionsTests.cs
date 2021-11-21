using AutoFixture;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Extensions;
using System.Collections.Generic;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    public class DeliveryOptionsExtensionsTests
    {
        private static readonly Fixture _fixture = new Fixture();

        [Theory]
        [MemberData(nameof(GetDeliveryOptionsData))]
        public void Configure(DeliveryOptions options)
        {
            var o = new DeliveryOptions();

            o.Configure(options);

            o.Should().BeEquivalentTo(options);
        }

        public static IEnumerable<object[]> GetDeliveryOptionsData()
        {
            yield return new object[] { _fixture.Create<DeliveryOptions>() };
            yield return new object[] { _fixture.Create<DeliveryOptions>() };
        }
    }
}
