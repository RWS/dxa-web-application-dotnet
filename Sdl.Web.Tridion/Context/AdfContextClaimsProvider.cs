using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Tridion.ContentDelivery.AmbientData;

namespace Sdl.Web.Tridion.Context
{
    public class AdfContextClaimsProvider : IContextClaimsProvider
    {
        private const string ContextClaimPrefix = "taf:claim:context:";

        #region IContextClaimsProvider Members

        /// <summary>
        /// Gets the context claims. Either all context claims or for a given aspect name.
        /// </summary>
        /// <param name="aspectName">The aspect name. If <c>null</c> all context claims are returned.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>A dictionary with the claim names in format aspectName.propertyName as keys.</returns>
        public IDictionary<string, object> GetContextClaims(string aspectName, Localization localization)
        {
            using (new Tracer(aspectName))
            {
                string claimNamePrefix = ContextClaimPrefix;
                if (!string.IsNullOrEmpty(aspectName))
                {
                    claimNamePrefix += aspectName + ":";
                }

                IDictionary<string, object> result = new Dictionary<string, object>();
                foreach (KeyValuePair<Uri, object> claim in AmbientDataContext.CurrentClaimStore.GetAll())
                {
                    string claimName = claim.Key.ToString();
                    if (!claimName.StartsWith(claimNamePrefix))
                    {
                        continue;
                    }
                    string propertyName = claimName.Substring(ContextClaimPrefix.Length).Replace(':', '.');
                    result.Add(propertyName, claim.Value);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the device family (an aggregated device claim determined from other context claims).
        /// </summary>
        /// <returns>A string representing the device family.</returns>
        public string GetDeviceFamily()
        {
            return null;
        }

        #endregion
    }
}
