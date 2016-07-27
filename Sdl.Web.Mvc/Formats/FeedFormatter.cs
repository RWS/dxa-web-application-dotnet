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
            IEnumerable<FeedItem> entityItems = GetEntityItems(entity);
            foreach(FeedItem item in entityItems)
            {
                items.Add(GetSyndicationItemFromFeedItem(item));
            }
            return items;
        }

        private Link CreateLink(object link)
        {
            if (link == null)
                return null;
            
            if(link is string)
            {
                return new Link { Url = link as string };
            }
            else if(link is Link)
            {
                return link as Link;
            }
            return null;
        }

        private IEnumerable<FeedItem> GetEntityItems(EntityModel entity)
        {
            List<FeedItem> res = new List<FeedItem>();
            // if the entity is an actual FeedItem we just add it directly.
            if (entity is FeedItem)
            {
                res.Add((FeedItem)entity);
            }
            else
            {
                // if the entity type is (semantically) a list, get its list items
                List<FeedItem> items = GetFeedItemListFromSemantics(entity);
                if (items != null)
                {
                    res = items;
                }
                else
                {
                    // the entity we are dealing with is not known so try to find some suitable properties using reflection
                    FeedItem feedItem = new FeedItem();
                    foreach (PropertyInfo pi in entity.GetType().GetProperties())
                    {
                        switch (pi.Name)
                        {
                            case "Title":
                            case "Headline":
                            case "Name":
                                feedItem.Title = pi.GetValue(entity) as String;
                                break;
                            case "Summary":
                            case "Description":
                            case "Snippet":
                            case "Teaser":
                                feedItem.Summary = pi.GetValue(entity) as String;
                                break;                            
                            case "Updated":
                            case "Date":
                                DateTime? date = pi.GetValue(entity) as DateTime?;
                                if (date != null)
                                    feedItem.Date = date;
                                break;                           
                            case "Link":
                            case "Url":
                                feedItem.Link = CreateLink(pi.GetValue(entity));
                                break;
                        }
                    }
                    // only use the feed item if some properties were populated
                    if (feedItem.Title != null || feedItem.Summary != null || feedItem.Link != null)
                    {
                        res.Add(feedItem);
                    }
                }
            }
            return res;
        }

        private List<FeedItem> GetFeedItemListFromSemantics(EntityModel entity)
        {
            Type type = entity.GetType();
            bool isList = false;
            foreach (object attr in type.GetCustomAttributes(true))
            {
                if (attr is SemanticEntityAttribute)
                {
                    SemanticEntityAttribute semantics = (SemanticEntityAttribute)attr;
                    isList = semantics.Vocab == ViewModel.SchemaOrgVocabulary && semantics.EntityName == "ItemList";
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
                    if (pi.PropertyType == typeof(List<FeedItem>))
                    {
                         return pi.GetValue(entity) as List<FeedItem>;
                    }
                }
            }
            return null;
        }

        private SyndicationItem GetSyndicationItemFromFeedItem(FeedItem item)
        {
            SyndicationItem si = new SyndicationItem();
            if (item.Title != null)
            {
                si.Title = new TextSyndicationContent(item.Title);
            }
            if (item.Summary != null)
            {
                si.Summary = new TextSyndicationContent(item.Summary.ToString());
            }
            if (item.Link != null && item.Link.Url != null && item.Link.Url.StartsWith("http"))
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
