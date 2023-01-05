﻿using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection
{
    internal class NamedDeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly INamedServiceProvider _namedServiceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleDeliveryClientFactory"/> class.
        /// </summary>
        /// <param name="deliveryOptions">Used for notifications when <see cref="DeliveryOptions"/> instances change.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        /// <param name="namedServiceProvider">A named service provider.</param>
        public NamedDeliveryClientFactory(IOptionsMonitor<DeliveryOptions> deliveryOptions, IServiceProvider serviceProvider,
            INamedServiceProvider namedServiceProvider)
        {
            _deliveryOptions = deliveryOptions;
            _serviceProvider = serviceProvider;
            _namedServiceProvider = namedServiceProvider;
        }

        /// <inheritdoc />
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                var deliveryClientOptions = _deliveryOptions.Get(name);
                if (deliveryClientOptions.Name == name)
                {
                    client = Build(deliveryClientOptions, name);

                    _cache.TryAdd(name, client);
                }
            }

            return client;
        }

        public IDeliveryClient Get() => GetService<IDeliveryClient>();

        private IDeliveryClient Build(DeliveryOptions options, string name)
        {
            return Delivery.DeliveryClientFactory.Create(
                new DeliveryOptionsMonitor(options, name),
                GetNamedServiceOrDefault<IModelProvider>(name),
                GetNamedServiceOrDefault<IRetryPolicyProvider>(name),
                GetNamedServiceOrDefault<ITypeProvider>(name),
                GetNamedServiceOrDefault<IDeliveryHttpClient>(name),
                GetNamedServiceOrDefault<JsonSerializer>(name));
        }

        private T GetNamedServiceOrDefault<T>(string name)
        {
            var service = _namedServiceProvider.GetService<T>(name);
            if (service == null)
            {
                service = GetService<T>();
            }

            return service;
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }
    }
}
