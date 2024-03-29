﻿using System;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Context of the current resolving process
    /// </summary>
    public class ResolvingContext
    {
        /// <summary>
        /// Gets the content item within current resolving context
        /// </summary>
        public Func<string, Task<object>> GetLinkedItem { get; internal set; }

        /// <summary>
        /// Gets an instance that resolves content links in Rich text element values
        /// </summary>
        public IContentLinkUrlResolver ContentLinkUrlResolver { get; internal set; }
    }
}
