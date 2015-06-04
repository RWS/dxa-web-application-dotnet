using System.Collections.Specialized;
using System.Reflection;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Web;

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

        protected virtual List<SyndicationItem> GetFeedItemsFromEntity(EntityModel entity)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();
            IEnumerable<Teaser> entityItems = GetEntityItems(entity);
            foreach(Teaser item in entityItems)
            {
                items.Add(GetSyndicationItemFromTeaser(item));
            }
            return items;
        }

        private IEnumerable<Teaser> GetEntityItems(EntityModel entity)
        {
            List<Teaser> res = new List<Teaser>();
            //1. Check if entity is a teaser, if add it
            if (entity is Teaser)
            {
                res.Add((Teaser)entity);
            }
            else
            {
                //2. Second check if entity type is (semantically) a list, and if so, get its list items
                List<Teaser> items = GetTeaserListFromSemantics(entity);
                if (items != null)
                {
                    res = items;
                }
                else
                {
                    //3. Last resort, try to find some suitable properties using reflection
                    Teaser teaser = new Teaser();
                    foreach (PropertyInfo pi in entity.GetType().GetProperties())
                    {
                        switch (pi.Name)
                        {
                            case "Headline":
                            case "Name":
                                teaser.Headline = pi.GetValue(entity) as String;
                                break;
                            case "Date":
                                DateTime? date = pi.GetValue(entity) as DateTime?;
                                if (date != null)
                                    teaser.Date = date;
                                break;
                            case "Description":
                                teaser.Text = pi.GetValue(entity) as String;
                                break;
                            case "Link":
                                teaser.Link = pi.GetValue(entity) as Link;
                                break;
                            case "Url":
                                string url = pi.GetValue(entity) as String;
                                if (url != null)
                                    teaser.Link = new Link { Url = url };
                                break;
                        }
                    }
                    if (teaser.Headline != null || teaser.Text != null || teaser.Link != null)
                    {
                        res.Add(teaser);
                    }
                }
            }
            return res;
        }

        private List<Teaser> GetTeaserListFromSemantics(EntityModel entity)
        {
            Type type = entity.GetType();
            bool isList = false;
            foreach (object attr in type.GetCustomAttributes(true))
            {
                if (attr is SemanticEntityAttribute)
                {
                    SemanticEntityAttribute semantics = (SemanticEntityAttribute)attr;
                    isList = semantics.Vocab == "http://schema.org/" && semantics.EntityName == "ItemList";
                    if (isList)
                    {
                        break;
                    }
                }
            }
            if (isList)
            {
                foreach(PropertyInfo pi in type.GetProperties())
                {
                    if (pi.PropertyType == typeof(List<Teaser>))
                    {
                         return pi.GetValue(entity) as List<Teaser>;
                    }
                }
            }
            return null;
        }

        private SyndicationItem GetSyndicationItemFromTeaser(Teaser item)
        {
            SyndicationItem si = new SyndicationItem();
            if (item.Headline != null)
            {
                si.Title = new TextSyndicationContent(item.Headline);
            }
            if (item.Text != null)
            {
                si.Summary = new TextSyndicationContent(item.Text);
            }
            if (item.Link != null && item.Link.Url!=null && item.Link.Url.StartsWith("http"))
            {
                si.Links.Add(new SyndicationLink(new Uri(item.Link.Url)));
            }
            if (item.Date != null)
            {
                si.PublishDate = (DateTime)item.Date;
            }
            return si;
        }
    }
}
