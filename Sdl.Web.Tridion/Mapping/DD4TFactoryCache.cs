using System.Collections.Generic;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Cache for DD4T Factories; one for each Localization.
    /// </summary>
    internal static class DD4TFactoryCache
    {
        private static readonly IDictionary<string, IPageFactory> _pageFactories = new Dictionary<string, IPageFactory>();
        private static readonly IDictionary<string, IComponentFactory> _componentFactories = new Dictionary<string, IComponentFactory>();
        private static readonly IDictionary<string, IBinaryFactory> _binaryFactories = new Dictionary<string, IBinaryFactory>();

        internal static IPageFactory GetPageFactory(Localization localization)
        {
            lock (_pageFactories)
            {
                IPageFactory pageFactory;
                if (!_pageFactories.TryGetValue(localization.LocalizationId, out pageFactory))
                {
                    pageFactory = new PageFactory
                    {
                        PublicationResolver = new PublicationResolver(localization)
                    };
                    _pageFactories.Add(localization.LocalizationId, pageFactory);
                }

                return pageFactory;
            }
        }

        internal static IComponentFactory GetComponentFactory(Localization localization)
        {
            lock (_componentFactories)
            {
                IComponentFactory componentFactory;
                if (!_componentFactories.TryGetValue(localization.LocalizationId, out componentFactory))
                {
                    componentFactory = new ComponentFactory()
                    {
                        PublicationResolver = new PublicationResolver(localization)
                    };
                    _componentFactories.Add(localization.LocalizationId, componentFactory);
                }

                return componentFactory;
            }
        }

        internal static IBinaryFactory GetBinaryFactory(Localization localization)
        {
            lock (_binaryFactories)
            {
                IBinaryFactory binaryFactory;
                if (!_binaryFactories.TryGetValue(localization.LocalizationId, out binaryFactory))
                {
                    binaryFactory = new BinaryFactory()
                    {
                        PublicationResolver = new PublicationResolver(localization)
                    };
                    _binaryFactories.Add(localization.LocalizationId, binaryFactory);
                }

                return binaryFactory;
            }
        }

    }
}
