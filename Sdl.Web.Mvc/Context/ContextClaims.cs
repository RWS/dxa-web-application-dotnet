using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.Mvc.Context
{
    /// <summary>
    /// Abstract base class for strongly typed context claims classes (such as <see cref="DeviceClaims"/>, <see cref="BrowserClaims"/> and <see cref="OperatingSystemClaims"/>).
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </remarks>
    public abstract class ContextClaims
    {
        private IDictionary<string, object> _claims;

        /// <summary>
        /// Sets the underlying claims as obtained from the <see cref="Sdl.Web.Common.Interfaces.IContextClaimsProvider"/>
        /// </summary>
        /// <param name="claims">The claims as obtained from the <see cref="Sdl.Web.Common.Interfaces.IContextClaimsProvider"/></param>
        /// <remarks>This method is invoked by the <see cref="ContextEngine"/> immediately after constructing the strongly types context claims class (using a parameterless constructor).</remarks>
        protected internal virtual void SetClaims(IDictionary<string, object> claims)
        {
            _claims = claims;
        }

        /// <summary>
        /// Gets the name of the "aspect" which the strongly typed claims class represents.
        /// </summary>
        /// <returns>The name of the aspect.</returns>
        /// <remarks>This method must be overridden in a concrete subclass. Concrete subclasses represent one aspect.</remarks>
        protected internal abstract string GetAspectName();

        /// <summary>
        /// Gets the (qualified) claim name in format aspectName.propertyName.
        /// </summary>
        /// <param name="properyName">The name of the property.</param>
        /// <returns>The (qualified) claim name in format aspectName.propertyName.</returns>
        protected string GetClaimName(string properyName) => $"{GetAspectName()}.{properyName}";

        /// <summary>
        /// Get a typed value of a claim with a given property name.
        /// </summary>
        /// <typeparam name="T">The type of the claim value.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the claim.</returns>
        public T GetClaimValue<T>(string propertyName)
        {
            object claimValue;
            _claims.TryGetValue(GetClaimName(propertyName), out claimValue);
            return CastValue<T>(claimValue);
        }

        /// <summary>
        /// Get typed values of a claim with a given property name.
        /// </summary>
        /// <typeparam name="T">The type of the individual claim values; an enumerable of this type will be returned.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The values of the claim.</returns>
        public T[] GetClaimValues<T>(string propertyName)
        {
            object claimValue;
            _claims.TryGetValue(GetClaimName(propertyName), out claimValue);
            return (claimValue == null) ? null : (from object item in (IEnumerable) claimValue select CastValue<T>(item)).ToArray();
        }

        internal static T CastValue<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }
                
            if (value is T)
            {
                return (T) value;
            }

            if (typeof(T) == typeof(string))
            {
                return (T) (object) value.ToString();
            }

            return (T) Convert.ChangeType(value, typeof(T));
        }
    }
}
