using System;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Models;

namespace Sdl.Web.Mvc.Html
{
    public static class HtmlHelperExtensions
    {
        

        public static string Date(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            return date != null ? ((DateTime)date).ToString(format, new CultureInfo(Configuration.GetConfig("core.culture"))) : null;
        }

        public static string DateDiff(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            //TODO make the text come from resources
            if (date!=null)
            {
                int dayDiff = (int)(DateTime.Now.Date - ((DateTime)date).Date).TotalDays;
                if (dayDiff <= 0)
                {
                    return htmlHelper.Resource("core.todayText");
                }
                if (dayDiff == 1)
                {
                    return htmlHelper.Resource("core.yesterdayText");
                }
                if (dayDiff <= 7)
                {
                    return String.Format(htmlHelper.Resource("core.xDaysAgoText"), dayDiff);
                }
                else
                {
                    return ((DateTime)date).ToString(format, new CultureInfo(Configuration.GetConfig("core.culture")));
                }
            }
            return null;
        }

        public static string FormatResource(this HtmlHelper htmlHelper, string resourceName, params object[] parameters)
        {
            return String.Format((string)htmlHelper.Resource(resourceName),parameters);
        }

        public static string Resource(this HtmlHelper htmlHelper, string resourceName)
        {
            return (string)Resource(htmlHelper.ViewContext.HttpContext, resourceName);
        }

        public static object FormatResource(this HttpContextBase httpContext, string resourceName, params object[] parameters)
        {
            return String.Format((string)httpContext.Resource(resourceName), parameters);
        }

        public static object Resource(this HttpContextBase httpContext, string resourceName)
        {
            return httpContext.GetGlobalResourceObject(CultureInfo.CurrentUICulture.ToString(), resourceName);
        }

        public static object FriendlyFileSize(this HtmlHelper httpContext, long sizeInBytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = sizeInBytes;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            return String.Format("{0} {1}", Math.Ceiling(len), sizes[order]);
        }
    }
}
