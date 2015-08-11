using System;
using System.Collections.Concurrent;

namespace Sdl.Web.Tridion.Statics
{
    /// <summary>
    /// NamedLocker using ConcurrentDictionary.
    /// Based on http://johnculviner.com/achieving-named-lock-locker-functionality-in-c-4-0/
    /// </summary>
    /// <remarks>
    /// This works fine for multiple threads, but it doesn't work for multiple processes.
    /// </remarks>
    internal static class NamedLocker
    {
        private static readonly ConcurrentDictionary<string, object> Locks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Get a lock for use with a lock(){} block.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        /// <returns>The lock object</returns>
        public static Object GetLock(string name)
        {
            return Locks.GetOrAdd(name, s => new object());
        }

        /// <summary>
        /// Remove an old lock object that is no longer needed.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        public static void RemoveLock(string name)
        {
            object o;
            Locks.TryRemove(name, out o);
        }
    }
}
