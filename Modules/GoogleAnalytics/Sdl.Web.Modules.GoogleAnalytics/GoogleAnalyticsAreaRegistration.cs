using Sdl.Web.Mvc.Configuration;
using System;
using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.GoogleAnalytics
{
    public class GoogleAnalyticsAreaRegistration : BaseAreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "GoogleAnalytics";
            }
        }
        protected override void RegisterAllViewModels()
        {
            RegisterViewModel("GoogleAnalytics", typeof(Sdl.Web.Common.Models.Configuration));
        }
        
    }
}