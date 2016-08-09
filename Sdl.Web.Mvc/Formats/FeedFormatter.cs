using System.Collections.Specialized;
using System.Reflection;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Web;
using System.Collections;

namespace Sdl.Web.Mvc.Formats
{
    public abstract class FeedFormatter : BaseFormatter
    {
        protected SyndicationFeed GetData(object model)
        {
            PageModel page = model as PageModel;
            if (page!=null)
            {
                string description = page.Meta.ContainsKey("description") ? page.Meta["description"] : null;
                string url = RemoveFormatFromUrl();
                SyndicationFeed feed = new SyndicationFeed(page.Title, description, new Uri(url))
                {
                    Language = WebRequestContext.Localization.Culture,
                    Items = GetFeedItemsFromPage(page)
                };
                return feed;
            }
            return null;
        }

        private string RemoveFormatFromUrl()
        {
            NameValueCollection filtered = HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());
            filtered.Remove("format");
            return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path) + (filtered.Count>0 ?  "?" + filtered : "");
        }

        private IEnumerable<SyndicationItem> GetFeedItemsFromPage(PageModel page)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();
            foreach (RegionModel region in page.Regions)
            {
                foreach (EntityModel entity in region.Entities)
                {
                    items.AddRange(GetFeedItemsFromEntity(entity));
                }
            }
            return items;
        }     
    
        private IEnumerable<SyndicationItem> GetFeedItemsFromEntity(EntityModel entity)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();

            if (entity != null)
            {
                Type type = entity.GetType();
                foreach (PropertyInfo pi in type.GetProperties())
                {
                    if (pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>) || pi.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) &&
                        pi.PropertyType.GenericTypeArguments.Length > 0 && typeof(EntityModel).IsAssignableFrom(pi.PropertyType.GenericTypeArguments[0]))
                    {
                        ICollection subItems = pi.GetValue(entity) as ICollection;
                        if (subItems != null)
                        {
                            foreach (object e in subItems)
                            {
                                items.AddRange(GetFeedItemsFromEntity(e as EntityModel));
                            }
                        }
                    }
                }
                SyndicationItem item = CreateFeedItemFromProperties(entity);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private Link CreateLink(object link)
        {
            if (link == null)
                return null;

            if (link is string)
            {
                return new Link { Url = link as string };
            }
            else if (link is Link)
            {
                return link as Link;
            }
            return null;
        }

        private string GetText(object obj)
        {
            if (obj is string)
                return obj as string;
            if (obj is RichText)
                return (obj as RichText).ToString();
            return null;
        }

        private SyndicationItem CreateFeedItemFromProperties(EntityModel entity)
        {
            if (entity == null)
                return null;

            string title = null;
            string summary = null;
            DateTime? date = null;
            Link link = null;

            foreach (PropertyInfo pi in entity.GetType().GetProperties())
            {
                switch (pi.Name)
                {
                    case "Title":
                    case "Headline":
                    case "Name":
                        title = GetText(pi.GetValue(entity));
                        break;
                    case "Summary":
                    case "Description":
                    case "Snippet":
                    case "Teaser":
                    case "Text":
                        summary = GetText(pi.GetValue(entity));
                        break;
                    case "Updated":
                    case "Date":
                        date = pi.GetValue(entity) as DateTime?;
                        break;
                    case "Link":
                    case "Url":
                        link = CreateLink(pi.GetValue(entity));
                        break;
                }
            }

            // only create a syndication if we found a property
            if (title != null || summary != null || link != null)
            {
                SyndicationItem si = new SyndicationItem();
                if (title != null)
                {
                    si.Title = new TextSyndicationContent(title);
                }
                if (summary != null)
                {
                    si.Summary = new TextSyndicationContent(summary.ToString());
                }
                if (link != null && link.Url != null && link.Url.StartsWith("http"))
                {
                    si.Links.Add(new SyndicationLink(new Uri(link.Url)));
                }
                if (date != null)
                {
                    si.PublishDate = (DateTime)date;
                }
                return si;
            }
            return null;
        }
    }
}
