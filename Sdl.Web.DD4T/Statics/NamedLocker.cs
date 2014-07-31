using System;
using System.Collections.Concurrent;

namespace Sdl.Web.DD4T.Statics
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
        private static readonly ConcurrentDictionary<string, Object> Locks = new ConcurrentDictionary<string, Object>();

        /// <summary>
        /// Get a lock for use with a lock(){} block.
        /// </summary>
        /// <remarks>
        /// Since we only need write locking, we will just return a new Object which essentially is no lock at all.
        /// </remarks>
        /// <param name="name">Name of the lock</param>
        /// <returns>The lock object</returns>
        public static Object GetReadLock(string name)
        {
            return new Object();
        }

        /// <summary>
        /// Get a lock for use with a lock(){} block.
        /// </summary>
        /// <remarks>
        /// Since we only need write locking, we will just return a new Object which essentially is no lock at all.
        /// </remarks>
        /// <param name="name">Name of the lock</param>
        /// <returns>The lock object</returns>
        public static Object GetDeleteLock(string name)
        {
            return new Object();
        }

        /// <summary>
        /// Get a lock for use with a lock(){} block.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        /// <returns>The lock object</returns>
        public static Object GetWriteLock(string name)
        {
            return Locks.GetOrAdd(name, s => new Object());
        }

        /// <summary>
        /// Remove an old lock object that is no longer needed.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        public static void RemoveLock(string name)
        {
            Object o;
            Locks.TryRemove(name, out o);
        }
    }
}
