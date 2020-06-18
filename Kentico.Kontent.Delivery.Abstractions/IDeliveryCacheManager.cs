using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//TODO: comments
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
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null) where T : class;

        /// <summary>
        /// Attemptes to retrieve data from cache.
        /// </summary>
        /// <typeparam name="T">Type of the response used for deserialization</typeparam>
        /// <param name="key">A cache key</param>
        /// <returns>Returns true along with the deserialized value if the retrieval attempt was successful. Otherwise, returns false and null for the value.</returns>
        Task<(bool Success, T Value)> TryGetAsync<T>(string key) where T : class;

        /// <summary>
        /// Invalidates data by a cache key.
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
