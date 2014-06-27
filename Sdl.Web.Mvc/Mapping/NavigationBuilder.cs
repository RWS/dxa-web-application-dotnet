using Sdl.Web.Mvc.Common;
using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public class NavigationBuilder
    {
        public IContentProvider ContentProvider { get; set; }
        public string NavigationUrl { get; set; }

        public NavigationLinks BuildContextNavigation(string requestUrl)
        {
            NavigationLinks links = new NavigationLinks();
            SitemapItem parent = (SitemapItem)ContentProvider.GetNavigationModel(NavigationUrl);
            int levels = requestUrl.Split('/').Length;
            while (levels > 1 && parent.Items != null)
            {
                var newParent = parent.Items.Where(i => i.Type=="StructureGroup" && requestUrl.StartsWith(i.Url.ToLower())).FirstOrDefault();
                if (newParent == null)
                {
                    break;
                }
                parent = newParent;
            }
            foreach (var item in parent.Items.Where(i => i.Visible))
            {
                links.Items.Add(GetLink(item));
            }
            return links;
        }

        public NavigationLinks BuildBreadcrumb(string requestUrl)
        {
            NavigationLinks breadcrumb = new NavigationLinks();
            int levels = requestUrl.Split('/').Length;
            SitemapItem parent = (SitemapItem)ContentProvider.GetNavigationModel(NavigationUrl);
            breadcrumb.Items.Add(GetLink(parent));
            while (levels > 1 && parent.Items != null)
            {
                parent = parent.Items.Where(i => requestUrl.StartsWith(i.Url.ToLower())).FirstOrDefault();
                if (parent != null)
                {
                    breadcrumb.Items.Add(GetLink(parent));
                    levels--;
                }
                else
                {
                    break;
                }
            }
            return breadcrumb;
        }

        public NavigationLinks BuildTopNavigation(string requestUrl)
        {
            NavigationLinks links = new NavigationLinks();
            SitemapItem parent = (SitemapItem)ContentProvider.GetNavigationModel(NavigationUrl);
            foreach (var item in parent.Items.Where(i => i.Visible))
            {
                links.Items.Add(GetLink((item.Title == "Index") ? parent : item));
            }
            return links;
        }

        private Link GetLink(SitemapItem sitemapItem)
        {
            return new Link { Url = ContentProvider.ProcessUrl(sitemapItem.Url), LinkText = sitemapItem.Title };
        }
    }
}
