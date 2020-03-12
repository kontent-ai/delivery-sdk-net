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
        /// Returns or Adds data to the cache
        /// </summary>
        /// <typeparam name="T">A generic type</typeparam>
        /// <param name="key">A cache key</param>
        /// <param name="valueFactory">A factory which returns a data</param>
        /// <param name="shouldCache"></param>
        /// <param name="dependenciesFactory"></param>
        /// <returns>The data of a generic type</returns>
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null);

        /// <summary>
        /// Tries to return a data
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="key">A cache key</param>
        /// <param name="value">Returns data in out parameter if are there.</param>
        /// <returns>Returns true or false.</returns>
        Task<bool> TryGetAsync<T>(string key, out T value);

        /// <summary>
        /// Invalidates data by the key
        /// </summary>
        /// <param name="key">A cache key</param>
        /// <returns></returns>
        Task InvalidateDependencyAsync(string key);

        /// <summary>
        /// Clears cache
        /// </summary>
        /// <returns></returns>
        Task ClearAsync();
    }
}
