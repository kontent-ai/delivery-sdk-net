using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KenticoKontent.Delivery.InlineContentItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KenticoKontent.Delivery.Tests.Extensions
{
    /// <summary>
    /// Mock ServiceCollection that keeps inside everything that was registered.
    /// Can be used to check whether expected instances were added to the ServiceCollection.
    /// </summary>
    internal class FakeServiceCollection : IServiceCollection
    {
        private readonly IList<ServiceDescriptor> _serviceDescriptors = new List<ServiceDescriptor>();

        /// <summary>
        /// Key represents contract (usually, an interface that was registered)
        /// Value represents implementation (type of instance that was registered for given contract (key))
        /// Supported descriptors either need instance or implementation type filled for this collection to work right
        /// </summary>
        internal IDictionary<Type, Type> Dependencies => _serviceDescriptors
            .ToDictionary(
                descriptor => descriptor.ServiceType,
                descriptor => descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType());

        /// <summary>
        /// Represent content item types that can be resolved by registered inline content item resolvers
        /// </summary>
        internal IList<Type> ContentTypesResolvedByResolvers { get; } = new List<Type>();

        /// <summary>
        /// Project ID specified in <see cref="IOptions"/> or <see cref="IConfigureOptions{TOptions}"/> 
        /// passed to the client builder or <see cref="IServiceCollection"/> extension.
        /// </summary>
        internal string ProjectId;

        private Type _resolvableContentType;

        public IEnumerator<ServiceDescriptor> GetEnumerator()
            => Enumerable.Empty<ServiceDescriptor>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Fills <see cref="Dependencies"/> dictionary that can be used in test assertions
        /// </summary>
        public void Add(ServiceDescriptor item)
        {
            // From CI perspective, to ensure their injection, at minimum projectId property needs to be recorded
            // Also, instance (IOptions implementation) type is not under our control thus should not assert against it
            item = HandleInstanceOptions(item);
            item = HandleConfigurationOptions(item);

            // Inline content items resolution requires user to register something generic, but inline processor
            // must work with something non-generic, hence special handling is required
            item = HandleTypedInlineContentItemResolvers(item);
            item = HandleTypelessInlineContentItemResolvers(item);

            if (item != null)
            {
                _serviceDescriptors.Add(item);
            }
        }

        /// <summary>
        /// Handles options registered through a builder or service collection
        /// </summary>
        private ServiceDescriptor HandleInstanceOptions(ServiceDescriptor item)
        {
            if (item.ServiceType == typeof(IOptions<DeliveryOptions>))
            {
                var options = (IOptions<DeliveryOptions>)item.ImplementationInstance;
                ProjectId = options.Value.ProjectId;
                return new ServiceDescriptor(item.ServiceType, item.ServiceType, item.Lifetime);
            }

            return item;
        }

        /// <summary>
        /// Handles options registered as (json) configuration 
        /// </summary>
        private ServiceDescriptor HandleConfigurationOptions(ServiceDescriptor item)
        {
            if (item.ServiceType == typeof(IConfigureOptions<DeliveryOptions>))
            {
                var options = new DeliveryOptions();
                var configureOptions = (IConfigureOptions<DeliveryOptions>)item.ImplementationInstance;
                configureOptions.Configure(options);
                ProjectId = options.ProjectId;
            }

            return item;
        }

        /// <summary>
        /// Handles resolvers SDK user defines to be used by either <see cref="InlineContentItemProcessor"/>
        /// or a processor of his own.
        /// </summary>
        private ServiceDescriptor HandleTypedInlineContentItemResolvers(ServiceDescriptor item)
        {
            // check if the item being registered is an inline content item's resolver for some content type
            var isGenericInlineContentItemsResolver = item.ServiceType.IsGenericType
                && typeof(IInlineContentItemsResolver<>).IsAssignableFrom(item.ServiceType.GetGenericTypeDefinition());

            if (isGenericInlineContentItemsResolver)
            {
                // We have no ImplementationInstance (if registered as generic) and there is no need to create it,
                // actually since we only need a type the default InlineContentItemProcessor would work with
                // Since InlineContentItemProcessor requires typeless resolvers (non-generic) and its registration
                // is supposed to follow generic resolver's one, we cache the type into a local. It should do for
                // tests, but might be a root cause of some false-negative test (you have been warned).
                _resolvableContentType = item.ServiceType.GetGenericArguments().FirstOrDefault();
            }

            return item;
        }

        /// <summary>
        /// Handles resolvers derived from typed resolver SDK user registered.
        /// These are supposed to be primarily used by <see cref="InlineContentItemProcessor"/> only.
        /// </summary>
        /// <returns>
        /// Null for typeless resolvers since we want to check their registration via
        /// <see cref="ContentTypesResolvedByResolvers"/> rather than container registrations
        /// (since there is no way to obtain its content item type unless instantiated)
        /// </returns>
        private ServiceDescriptor HandleTypelessInlineContentItemResolvers(ServiceDescriptor item)
        {
            // we expect that typeless resolver is registered after typeful (generic) one
            // and _resolvableContentType was thus filled (see HandleTypedInlineContentItemResolvers)
            if (item.ServiceType == typeof(ITypelessInlineContentItemsResolver) && (item.ImplementationInstance != null || item.ImplementationFactory != null))
            {
                ContentTypesResolvedByResolvers.Add(_resolvableContentType);
                return null;
            }

            return item;
        }

        public void Clear() => throw new NotImplementedException();

        public bool Contains(ServiceDescriptor item) => throw new NotImplementedException();

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(ServiceDescriptor item) => throw new NotImplementedException();

        public int Count { get => throw new NotImplementedException(); }

        public bool IsReadOnly { get => throw new NotImplementedException(); }

        public int IndexOf(ServiceDescriptor item) => throw new NotImplementedException();

        public void Insert(int index, ServiceDescriptor item) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        public ServiceDescriptor this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
