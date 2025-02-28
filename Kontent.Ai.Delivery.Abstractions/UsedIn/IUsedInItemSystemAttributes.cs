﻿namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents system attributes of a parent content item.
    /// </summary>
    public interface IUsedInItemSystemAttributes : ISystemAttributes
    {
        /// <summary>
        /// Gets the language of the content item.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the codename of the content type, for example "article".
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Gets the codename of the content collection to which the content item belongs.
        /// </summary>
        public string Collection { get; }

        /// <summary>
        /// Gets the codename of the workflow which the content item is assigned to.
        /// </summary>
        public string Workflow { get; }

        /// <summary>
        /// Gets the codename of the workflow step which the content item is assigned to.
        /// </summary>
        public string WorkflowStep { get; }
    }
}
