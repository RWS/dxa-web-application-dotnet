using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public class NavigationBuilder
    {
        public string RequestUrl{get;set;}
        public NavigationBuilder(string requestUrl)
        {
            RequestUrl = requestUrl.Replace("\\", "/").ToLower();
        }
        public SitemapItem GetParentNode(SitemapItem rootItem)
        {
            SitemapItem parent = rootItem;
            int levels = RequestUrl.Split('/').Length;
            while (levels > 1 && parent.Items != null)
            {
                var newParent = parent.Items.Where(i => i.Type=="StructureGroup" && RequestUrl.StartsWith(i.Url.ToLower())).FirstOrDefault();
                if (newParent == null)
                {
                    break;
                }
                parent = newParent;
            }
            return parent;
        }
        public Breadcrumb BuildBreadcrumb(SitemapItem rootItem)
        {
            Breadcrumb breadcrumb = new Breadcrumb();
            int levels = RequestUrl.Split('/').Length;
            breadcrumb.Items.Add(GetLink(rootItem));
            SitemapItem parent = rootItem;
            while (levels > 1 && parent.Items!=null)
            {
                parent = parent.Items.Where(i => RequestUrl.StartsWith(i.Url.ToLower())).FirstOrDefault();
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

        private Link GetLink(SitemapItem sitemapItem)
        {
            return new Link { Url = sitemapItem.Url, LinkText = sitemapItem.Title };
        }
    }
}
