using System;
using DD4T.ContentModel.Factories;

namespace DD4T.Mvc
{
    [Obsolete]
    public static class FactoryService
    {
        public static IBinaryFactory BinaryFactory 
        { 
            get 
            {
                return ServiceLocator.GetInstance<IBinaryFactory>();
            }
        }

        public static ILinkFactory LinkFactory 
        {
            get
            {
                return ServiceLocator.GetInstance<ILinkFactory>();
            }
        }

        public static IComponentFactory ComponentFactory
        {
            get 
            {
                return ServiceLocator.GetInstance<IComponentFactory>();
            }
            
        }

        public static IPageFactory PageFactory
        {
            get 
            {
                return ServiceLocator.GetInstance<IPageFactory>();
            }
        }

     
        public static ITaxonomyFactory TaxonomyFactory
        {
            get
            {
                return ServiceLocator.GetInstance<ITaxonomyFactory>();
            }
        }
    }
}
