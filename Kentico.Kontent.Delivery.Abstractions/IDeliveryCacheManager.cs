using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Cache responses against the Kentico Kontent Delivery API.
    /// </summary>
    public interface IDeliveryCacheManager
    {
        /// <summary>
        /// Returns the cached data or fetches the data using a factory and caches it before returing.
        /// </summary>
        /// <typeparam name="T">The type of data to be returned.</typeparam>
        /// <param name="key">A cache key under which the data is or will be stored.</param>
        /// <param name="valueFactory">A factory which returns data.</param>
        /// <param name="shouldCache">Callback deciding whether or not to cache the data.</param>
        /// <param name="dependenciesFactory">Callback for building a collection of dependencies.</param>
        /// <returns>The data of a specified type</returns>
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null) where T : class;

        /// <summary>
        /// Attemptes to retrieve data from cache.
        /// </summary>
        /// <typeparam name="T">Type of the response used for deserialization</typeparam>
        /// <param name="key">A cache key under which the data is supposed to be stored.</param>
        /// <returns>Returns a flag  indicating success along with the deserialized value if the retrieval attempt was successful. Otherwise, returns false and null for the value.</returns>
        Task<(bool Success, T Value)> TryGetAsync<T>(string key) where T : class;

        /// <summary>
        /// Invalidates data using a cache key.
        /// </summary>
        /// <param name="key">A cache key the data under which is supposed to be invalidated.</param>
        /// <returns>Asynchrnous task used for invalidating a cache item.</returns>
        Task InvalidateDependencyAsync(string key);

        /// <summary>
        /// Clears the cache.
        /// </summary>
        /// <returns>Asynchronous task used for clearing the cache.</returns>
        Task ClearAsync();
    }
}
