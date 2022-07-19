using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Options;
using System;

namespace Kentico.Kontent.Delivery.Configuration
{
    /// <inheritdoc/>
    public class DeliveryOptionsMonitor : IOptionsMonitor<DeliveryOptions>
    {
        /// <inheritdoc/>
        public DeliveryOptions CurrentValue { get; }

        /// <summary>
        /// Gets the name of the specific instance of the configuration object.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Default constructor that allows access to <see cref="DeliveryOptions"/> via <see cref="IOptionsMonitor{TOptions}"/>
        /// </summary>
        /// <param name="deliveryOptions">Options object</param>
        /// <param name="name">Identifies the specific instance of the configuration object.</param>
        public DeliveryOptionsMonitor(DeliveryOptions deliveryOptions, string name)
        {
            CurrentValue = deliveryOptions;
            Name = name;
        }

        /// <inheritdoc/>
        public DeliveryOptions Get(string name)
        {
            if (name == Name)
            {
                return CurrentValue;
            }
            return null;
        }

        /// <inheritdoc/>
        public IDisposable OnChange(Action<DeliveryOptions, string> listener)
        {
            throw new NotImplementedException();
        }
    }
}
