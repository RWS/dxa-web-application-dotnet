//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using DD4T.ViewModels.Contracts;
//using Dynamic = DD4T.ContentModel;
//using DD4T.Mvc.SiteEdit;
//using DD4T.ContentModel;

//namespace DD4T.ViewModels.XPM
//{
//    /// <summary>
//    /// XPM Markup Service for the DD4T implementation
//    /// </summary>
//    public class XpmMarkupService : IXpmMarkupService
//    {
//        public string RenderXpmMarkupForField(IField field, int index = -1)
//        {
//            var result = index >= 0 ? SiteEditService.GenerateSiteEditFieldTag(field, index)
//                            : SiteEditService.GenerateSiteEditFieldTag(field);
//            return result ?? string.Empty;
//        }

//        public string RenderXpmMarkupForComponent(IComponentPresentation cp, string region = null)
//        {
//            return SiteEditService.GenerateSiteEditComponentTag(cp, region); 
//        }

//        public bool IsSiteEditEnabled(IItem item)
//        {
//            int publicationId = 0;
//            try
//            {
//                publicationId = new TcmUri(item.Id).PublicationId;
//            }
//            catch (Exception) 
//            {
//                publicationId = 0;
//            }
//            return IsSiteEditEnabled(publicationId);
//        }

//        public bool IsSiteEditEnabled(int publicationId)
//        {
//            var settings = SiteEditService.SiteEditSettings.FirstOrDefault(x => x.Key == publicationId.ToString());
//            return settings.Value == null ? false : settings.Value.Enabled;
//        }
//    }
//}
