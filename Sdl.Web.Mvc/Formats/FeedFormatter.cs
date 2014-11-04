using Sdl.Web.Common.Models;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Linq;
using System.Collections.Generic;
using System;
using Sdl.Web.Mvc.Configuration;
using System.Web;
using System.Collections.Specialized;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Sdl.Web.Mvc.Formats
{
    public abstract class FeedFormatter : BaseFormatter
    {
        protected SyndicationFeed GetData(object model)
        {
            var page = model as WebPage;
            if (page!=null)
            {
                var description = page.Meta.ContainsKey("description") ? page.Meta["description"] : null;
                var url = RemoveFormatFromUrl();
                SyndicationFeed feed = new SyndicationFeed(page.Title, description, new Uri(url));
                feed.Language = WebRequestContext.Localization.Culture;
                feed.Items = GetFeedItemsFromPage(page);
                return feed;
            }
            return null;
        }

        private string RemoveFormatFromUrl()
        {
            var filtered = HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());
            filtered.Remove("format");
            return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path) + (filtered.Count>0 ?  "?" + filtered : "");
        }

        private List<SyndicationItem> GetFeedItemsFromPage(WebPage page)
        {
            var items = new List<SyndicationItem>();
            foreach (var region in page.Regions.Values)
            {
                foreach (var entity in region.Items)
                {
                    if (entity is IEntity)
                    {
                        items.AddRange(GetFeedItemsFromEntity((IEntity)entity));
                    }
                }
            }
            return items;
        }

        protected virtual List<SyndicationItem> GetFeedItemsFromEntity(IEntity entity)
        {
            var items = new List<SyndicationItem>();
            List<Teaser> entityItems = GetEntityItems(entity);
            foreach(var item in entityItems)
            {
                items.Add(GetSyndicationItemFromTeaser(item));
            }
            return items;
        }

        private List<Teaser> GetEntityItems(IEntity entity)
        {
            var res = new List<Teaser>();
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
                    var teaser = new Teaser();
                    foreach (var pi in entity.GetType().GetProperties())
                    {
                        switch (pi.Name)
                        {
                            case "Headline":
                            case "Name":
                                teaser.Headline = pi.GetValue(entity) as String;
                                break;
                            case "Date":
                                var date = pi.GetValue(entity) as DateTime?;
                                if (date != null)
                                    teaser.Date = (DateTime)date;
                                break;
                            case "Description":
                                teaser.Text = pi.GetValue(entity) as String;
                                break;
                            case "Link":
                                teaser.Link = pi.GetValue(entity) as Link;
                                break;
                            case "Url":
                                var url = pi.GetValue(entity) as String;
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

        private List<Teaser> GetTeaserListFromSemantics(IEntity entity)
        {
            Type type = entity.GetType();
            bool isList = false;
            foreach (var attr in type.GetCustomAttributes(true))
            {
                if (attr is SemanticEntityAttribute)
                {
                    var semantics = (SemanticEntityAttribute)attr;
                    isList = semantics.Vocab == "http://schema.org" && semantics.EntityName == "ItemList";
                    if (isList)
                    {
                        break;
                    }
                }
            }
            if (isList)
            {
                foreach(var pi in type.GetProperties())
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
            var si = new SyndicationItem();
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
