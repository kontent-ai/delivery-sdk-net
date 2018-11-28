using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery.InlineContentItems
{
    internal class InlineContentItemsResolverCollection : IInlineContentItemsResolverCollection
    {
        private Dictionary<Type, ResolveInlineContent> _inlineContentItemsResolvers;
        public Dictionary<Type, ResolveInlineContent> InlineContentItemsResolvers
        {
            get
            {
                return _inlineContentItemsResolvers ?? (_inlineContentItemsResolvers = new Dictionary<Type, ResolveInlineContent>());
            }
        }

        public InlineContentItemsResolverCollection()
        {

        }
    }
}
