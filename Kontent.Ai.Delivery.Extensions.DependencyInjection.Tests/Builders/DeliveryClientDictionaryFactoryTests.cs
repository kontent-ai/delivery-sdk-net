using FakeItEasy;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection.Builders.Tests
{
    public class DeliveryClientDictionaryFactoryTests
    {
        private const string _clientName = "ClientName";

        private readonly ConcurrentDictionary<string, IDeliveryClient> _dictionary = new ConcurrentDictionary<string, IDeliveryClient>(
            new Dictionary<string, IDeliveryClient>
            {
                { _clientName, A.Fake<IDeliveryClient>() }
            }
        );

        [Fact]
        public void CreateFactory_NoClient_RaiseArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MultipleDeliveryClientFactory(null));
        }

        [Fact]
        public void GetNamedClient_WithCorrectName_GetClient()
        {
            var deliveryClientFactory = new MultipleDeliveryClientFactory(_dictionary);

            var result = deliveryClientFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Fact]
        public void GetNamedClient_WithWrongName_RaiseArgumentException()
        {
            var deliveryClientFactory = new MultipleDeliveryClientFactory(_dictionary);

            Assert.Throws<ArgumentException>(() => deliveryClientFactory.Get("wrongName"));
        }

        [Fact]
        public void GetClient_NoName_RaiseNotImplementedException()
        {
            var deliveryClientFactory = new MultipleDeliveryClientFactory(_dictionary);

            Assert.Throws<NotImplementedException>(() => deliveryClientFactory.Get());
        }
    }
}