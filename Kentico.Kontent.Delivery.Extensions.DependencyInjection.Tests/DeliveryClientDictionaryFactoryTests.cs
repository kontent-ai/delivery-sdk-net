using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests
{
    public class DeliveryClientDictionaryFactoryTests
    {
        private const string _clientName = "ClientName";

        private readonly static IDeliveryClient _fakeClient = A.Fake<IDeliveryClient>();

        private readonly ConcurrentDictionary<string, IDeliveryClient> _dictionary = new ConcurrentDictionary<string, IDeliveryClient>(
            new Dictionary<string, IDeliveryClient>
            {
                { _clientName, _fakeClient }
            }
        );

        [Fact]
        public void CreateFactory_NoClient_RaiseArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DeliveryClientDictionaryFactory(null));
        }

        [Fact]
        public void GetNamedClient_WithCorrectName_GetClient()
        {
            var fakeClient = A.Fake<IDeliveryClient>();
            var deliveryClientFactory = new DeliveryClientDictionaryFactory(_dictionary);

            var result = deliveryClientFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Fact]
        public void GetNamedClient_WithWrongName_RaiseArgumentException()
        {
            var fakeClient = A.Fake<IDeliveryClient>();
            var deliveryClientFactory = new DeliveryClientDictionaryFactory(_dictionary);

            Assert.Throws<ArgumentException>(() => deliveryClientFactory.Get("wrongName"));
        }

        [Fact]
        public void GetClient_NoName_RaiseNotImplementedException()
        {
            var fakeClient = A.Fake<IDeliveryClient>();
            var deliveryClientFactory = new DeliveryClientDictionaryFactory(_dictionary);

            Assert.Throws<NotImplementedException>(() => deliveryClientFactory.Get());
        }
    }
}
