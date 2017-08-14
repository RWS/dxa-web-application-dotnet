using System;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Caching;
namespace DD4T.ContentModel.Factories
{
    public interface ILinkFactory
    {
        ILinkProvider LinkProvider { get; set; }
        ICacheAgent CacheAgent { get; set; }
        string ResolveLink(string componentUri);
        string ResolveLink(string sourcePageUri, string componentUri, string excludeComponentTemplateUri);
    }
}
