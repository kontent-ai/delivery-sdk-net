using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KenticoCloud.Delivery.Tests.Extensions
{
    internal class FakeServiceCollection : IServiceCollection
    {
        internal Dictionary<Type, Type> Dependencies =
            new Dictionary<Type, Type>();

        internal string ProjectId;

        public IEnumerator<ServiceDescriptor> GetEnumerator()
            => Enumerable.Empty<ServiceDescriptor>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ServiceDescriptor item)
        {
            if (item.ServiceType == typeof(IOptions<DeliveryOptions>))
            {
                var options = (IOptions<DeliveryOptions>) item.ImplementationInstance;
                ProjectId = options.Value.ProjectId;
            }

            if (item.ServiceType == typeof(IConfigureOptions<DeliveryOptions>))
            {
                var options = new DeliveryOptions();
                var configureOptions = (IConfigureOptions<DeliveryOptions>)item.ImplementationInstance;
                configureOptions.Configure(options);
                ProjectId = options.ProjectId;
            }

            Dependencies.Add(item.ServiceType, item.ImplementationType);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(ServiceDescriptor item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ServiceDescriptor item)
        {
            throw new NotImplementedException();
        }

        public int Count { get; }
        public bool IsReadOnly { get; }
        public int IndexOf(ServiceDescriptor item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public ServiceDescriptor this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
