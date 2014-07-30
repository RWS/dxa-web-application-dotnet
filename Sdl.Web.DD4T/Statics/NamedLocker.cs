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
        private static readonly ConcurrentDictionary<string, object> Locks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Get a lock for use with a lock(){} block.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        /// <returns>The lock object</returns>
        public static object GetLock(string name)
        {
            return Locks.GetOrAdd(name, s => new object());
        }

        /// <summary>
        /// Run a short lock inline using a lambda.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="name">Name of the lock</param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static TResult RunWithLock<TResult>(string name, Func<TResult> body)
        {
            lock (Locks.GetOrAdd(name, s => new object()))
            {
                return body();
            }
        }

        /// <summary>
        /// Run a short lock inline using a lambda.
        /// </summary>
        /// <param name="name">Name of the lock</param>
        /// <param name="body"></param>
        public static void RunWithLock(string name, Action body)
        {
            lock (Locks.GetOrAdd(name, s => new object()))
            {
                body();
            }
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
