namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    /// <summary>
    /// An enum represents a type of custom service provider.
    /// </summary>
    public enum CustomServiceProviderType
    {
        /// <summary>
        /// No custom service provider.
        /// </summary>
        None = 0,

        /// <summary>
        /// The autofac service provider.
        /// </summary>
        Autofac = 1
    }
}
