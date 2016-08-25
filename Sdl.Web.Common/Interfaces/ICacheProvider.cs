using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Cache Provider extension point.
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// Stores a given key/value pair in a given cache region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The value. If <c>null</c>, this effectively removes the key from the cache.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null);

        /// <summary>
        /// Tries to get a cached value for a given key and cache region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The cached value (output).</param>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <returns><c>true</c> if a cached value was found for the given key and cache region.</returns>
        bool TryGet<T>(string key, string region, out T value);

        /// <summary>
        /// Tries to gets a value for a given key and cache region. If not found, add a value obtained from a given function.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="addFunction">A function (delegate) used to obtain the value to add in case an existing cached value is not found.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to get or add.</typeparam>
        /// <remarks>
        /// This method is thread-safe; it prevents the same key being added by multiple threads in case of a race condition.
        /// </remarks>
        /// <returns>The cached value.</returns>
        T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null);
    }
}
