using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion.TridionDocs.Localization
{
    public class DocsLocalization : Common.Configuration.Localization
    {
        public override string Path { get; set; } = ""; // content path

        public override string CmUriScheme { get; } = "ish";

        public override bool IsXpmEnabled { get; set; } = false; // no xpm on dd-webapp      

        protected override void Load()
        {
            using (new Tracer(this))
            {
                LastRefresh = DateTime.Now;
            }
        }

        public override IDictionary GetResources(string sectionName = null)
            => new Hashtable(); // no resources so return empty hash to avoid default impl

        public override bool IsStaticContentUrl(string urlPath)
        {
            List<string> mediaPatterns = new List<string>();
            mediaPatterns.Add("^/favicon.ico");
            mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/assets/.*");
            mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/.*\\.json$");
            StaticContentUrlPattern = string.Join("|", mediaPatterns);
            Regex staticContentUrlRegex = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return staticContentUrlRegex.IsMatch(urlPath);
        }
    }
}
