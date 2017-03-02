﻿using System;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Facade for the legacy Content Resolver extension to support legacy clients of this interface.
    /// </summary>
#pragma warning disable 618
    public class LegacyContentResolverFacade : IContentResolver
#pragma warning restore 618
    {
        #region IContentResolver Members

        [Obsolete("Deprecated in DXA 1.1.")]
        public string DefaultExtensionLessPageName
        {
            get
            {
                return Constants.DefaultExtensionLessPageName;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1.");
            }
        }

        [Obsolete("Deprecated in DXA 1.1.")]
        public string DefaultPageName
        {
            get
            {
                return Constants.DefaultPageName;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1.");
            }
        }

        [Obsolete("Deprecated in DXA 1.1.")]
        public string DefaultExtension
        {
            get
            {
                return Constants.DefaultExtension;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1.");
            }
        }

        public string ResolveLink(object linkData, object resolveInstruction = null)
        {
            string localizationId = resolveInstruction as string;
            Localization contextLocalization = (localizationId == null) ? null : SiteConfiguration.LocalizationResolver.GetLocalization(localizationId);

            return SiteConfiguration.LinkResolver.ResolveLink((string) linkData, localization: contextLocalization);
        }

        [Obsolete("Deprecated in DXA 1.1.")]
        public object ResolveContent(object content, object resolveInstruction = null)
        {
            throw new NotSupportedException("ResolveContent is not supported in DXA 1.1.");
        }

        public MvcData ResolveMvcData(object data)
        {
            return ((ViewModel) data).MvcData;
        }

        #endregion
    }
}
