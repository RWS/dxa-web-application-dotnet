using System.Web.Mvc;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Logging;
using System.Text.RegularExpressions;
using DD4T.Factories;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel.Contracts.Configuration;
using System;
using DD4T.Core.Contracts.Resolvers;

namespace DD4T.Mvc.Html
{
    [Obsolete("Consider changing to ViewModels and rendering them with the RenderHelper")]
    public static class TridionHelper
    {
        private static IDD4TConfiguration _configuration;
        //private static ILinkFactory _linkFactory;
        private static ILinkResolver _linkResolver;
        private static ILogger _logger;
        private static IComponentPresentationRenderer _renderer;

        static TridionHelper()
        {
            //this is Anti-Pattern, there is no other way to inject dependencies into this class.
            //This helper should not be used in views, this logic should get executed by the controller.

            var config = DependencyResolver.Current.GetService<IDD4TConfiguration>();
            //var linkFactory = DependencyResolver.Current.GetService<ILinkFactory>();
            var logger = DependencyResolver.Current.GetService<ILogger>();
            var renderer = DependencyResolver.Current.GetService<IComponentPresentationRenderer>();
            var linkResolver = DependencyResolver.Current.GetService<ILinkResolver>();

            //_linkFactory = linkFactory;
            _configuration = config;
            _logger = logger;
            _renderer = renderer;
            _linkResolver = linkResolver;
        }

        public static MvcHtmlString RenderComponentPresentations(this HtmlHelper helper)
        {
            return RenderComponentPresentations(helper, null, null, null);
        }

        public static MvcHtmlString RenderComponentPresentations(this HtmlHelper helper, IComponentPresentationRenderer renderer)
        {
            return RenderComponentPresentations(helper, null, null, renderer);
        }

        public static MvcHtmlString RenderComponentPresentationsByView(this HtmlHelper helper, string byComponentTemplate, IComponentPresentationRenderer renderer)
        {
            if (string.IsNullOrEmpty(byComponentTemplate))
                return RenderComponentPresentations(helper, new string[] { }, null, renderer);
            else
                return RenderComponentPresentations(helper, new[] { byComponentTemplate }, null, renderer);
        }
        public static MvcHtmlString RenderComponentPresentationsByView(this HtmlHelper helper, string byComponentTemplate)
        {
            if (string.IsNullOrEmpty(byComponentTemplate))
                return RenderComponentPresentations(helper, new string[] { }, null, null);
            else
                return RenderComponentPresentations(helper, new[] { byComponentTemplate }, null, null);
        }


        public static MvcHtmlString RenderComponentPresentationsByView(this HtmlHelper helper, string[] byComponentTemplate, IComponentPresentationRenderer renderer)
        {
            return RenderComponentPresentations(helper, byComponentTemplate, null, renderer);
        }

        public static MvcHtmlString RenderComponentPresentationsByView(this HtmlHelper helper, string[] byComponentTemplate)
        {
            return RenderComponentPresentations(helper, byComponentTemplate, null, null);
        }

        public static MvcHtmlString RenderComponentPresentationsBySchema(this HtmlHelper helper, string bySchema, IComponentPresentationRenderer renderer)
        {
            return RenderComponentPresentations(helper, null, bySchema, renderer);
        }

        public static MvcHtmlString RenderComponentPresentationsBySchema(this HtmlHelper helper, string bySchema)
        {
            return RenderComponentPresentations(helper, null, bySchema, null);
        }

        public static MvcHtmlString RenderComponentPresentations(HtmlHelper helper, string[] byComponentTemplate, string bySchema, IComponentPresentationRenderer renderer)
        {
            _logger.Information(">>RenderComponentPresentations", LoggingCategory.Performance);
            IComponentPresentationRenderer cpr = renderer;
            IPage page = null;
            if (helper.ViewData.Model is IPage)
            {
                page = helper.ViewData.Model as IPage;
            }
            else
            {
                try
                {
                    page = helper.ViewContext.Controller.ViewBag.Page;
                }
                catch
                {
                    return new MvcHtmlString("<!-- RenderComponentPresentations can only be used if the model is an instance of IPage or if there is a Page property in the viewbag with type IPage -->");
                }
            }

            if (renderer == null)
            {
                _logger.Debug("about to create DefaultComponentPresentationRenderer", LoggingCategory.Performance);
                renderer = _renderer;
                _logger.Debug("finished creating DefaultComponentPresentationRenderer", LoggingCategory.Performance);
            }

            _logger.Debug("about to call renderer.ComponentPresentations", LoggingCategory.Performance);
            MvcHtmlString output = renderer.ComponentPresentations(page, helper, byComponentTemplate, bySchema);
            _logger.Debug("finished calling renderer.ComponentPresentations", LoggingCategory.Performance);
            _logger.Information("<<RenderComponentPresentations", LoggingCategory.Performance);

            return output;
        }

        #region linking functionality
        public static string GetResolvedUrl(this IComponent component)
        {
            return _linkResolver.ResolveUrl(component);
            //return _linkFactory.ResolveLink(component.Id);
        }

        public static MvcHtmlString GetResolvedLink(this IComponent component, string linkText, string showOnFail)
        {
            string url = component.GetResolvedUrl();
            if (string.IsNullOrEmpty(url))
                return new MvcHtmlString(showOnFail);
            return new MvcHtmlString(string.Format("<a href=\"{0}\">{1}</a>", url, linkText));
        }

        #endregion

        #region welcome file functionality
        public static string AddWelcomeFile(string url)
        {
            if (string.IsNullOrEmpty(url))
                return DefaultPageFileName;
            if (!reDefaultPage.IsMatch("/" + url))
                return url;
            if (url.EndsWith("/"))
                return url + DefaultPageFileName;
            return url + "/" + DefaultPageFileName;
        }

        private static string _defaultPageFileName = null;
        private static Regex reDefaultPage = new Regex(@".*/[^/\.]*(/?)$");
        public static string DefaultPageFileName
        {
            get
            {
                if (_defaultPageFileName == null)
                    _defaultPageFileName = _configuration.WelcomeFile;

                return _defaultPageFileName;
            }
        }
        #endregion
    }


}
