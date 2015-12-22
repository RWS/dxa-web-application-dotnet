using Newtonsoft.Json;

namespace Sdl.Web.Common.Models.Data
{
    /// <summary>
    /// Represents the (JSON) data for a Localization as stored in /system/config/_all.json.
    /// </summary>
    public class LocalizationData
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Language { get; set; }

        [JsonProperty("defaultLocalization")]
        public bool IsDefaultLocalization { get; set; }

        [JsonProperty("staging")]
        public bool IsXpmEnabled { get; set; }

        [JsonProperty("mediaRoot")]
        public string MediaRoot { get; set; }

        [JsonProperty("siteLocalizations")]
        public LocalizationData[] SiteLocalizations { get; set; }

        [JsonProperty("files")]
        public string[] ConfigStaticContentUrls { get; set; }

        // TODO: we're currently using "IsMaster" inside siteLocalizations, but "defaultLocalization" at top-level in _all.json.
        [JsonProperty("IsMaster")]
        private bool IsMaster
        {
            get { return IsDefaultLocalization; }
            set { IsDefaultLocalization = value; }
        }
    }


}
