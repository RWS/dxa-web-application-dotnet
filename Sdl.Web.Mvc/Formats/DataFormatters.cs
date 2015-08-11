using Sdl.Web.Mvc.Configuration;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public static class DataFormatters
    {
        public static Dictionary<string, IDataFormatter> Formatters { get; set; }
        static DataFormatters()
        {
            Formatters = new Dictionary<string, IDataFormatter>();
        }

        public static IDataFormatter GetFormatter(ControllerContext controllerContext)
        {
            string format = GetFormat(controllerContext);
            if (Formatters.ContainsKey(format) && WebRequestContext.Localization.DataFormats.Contains(format))
            {
                return Formatters[format];
            }
            return null;
        }

        public static List<string> GetValidTypes(ControllerContext controllerContext, List<string> allowedTypes)
        {
            List<string> res = new List<string>();
            string[] acceptTypes = controllerContext.HttpContext.Request.AcceptTypes;
            if (acceptTypes!=null)
            {
                foreach (string type in acceptTypes)
                {
                    foreach (string mediaType in allowedTypes)
                    {
                        if (type.Contains(mediaType))
                        {
                            res.Add(type);
                        }
                    }
                }
            }
            return res;
        }

        public static double GetScoreFromAcceptString(string type)
        {
            double res = 1.0;
            int pos = type.IndexOf("q=", System.StringComparison.Ordinal);
            if (pos > 0)
            {
                double.TryParse(type.Substring(pos + 2), out res);
            }
            return res;
        }

        private static string GetFormat(ControllerContext controllerContext)
        {
            string format = controllerContext.RequestContext.HttpContext.Request.QueryString["format"];
            if (format != null)
            {
                return format.ToLower();
            }
            format = "html";
            double topScore = GetHtmlAcceptScore(controllerContext);
            if (topScore<1.0)
            {
                foreach (string key in Formatters.Keys)
                {
                    double score = Formatters[key].Score(controllerContext);
                    if (score > topScore)
                    {
                        topScore = score;
                        format = key;
                    }
                    if (topScore == 1.0)
                    {
                        break;
                    }
                }
            }
            return format;
        }

        private static double GetHtmlAcceptScore(ControllerContext controllerContext)
        {
            double score = 0.0;
            string[] acceptTypes = controllerContext.HttpContext.Request.AcceptTypes;
            if (acceptTypes!=null)
            {
                foreach (string type in acceptTypes)
                {
                    if (type.Contains("html"))
                    {
                        double thisScore = GetScoreFromAcceptString(type);
                        if (thisScore > score)
                        {
                            score = thisScore;
                        }
                        if (score == 1)
                        {
                            break;
                        }
                    }
                }
            }
            return score;
        }
    }
}
