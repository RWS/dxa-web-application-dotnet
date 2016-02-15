using Newtonsoft.Json;

namespace Sdl.Web.Common.Models.Data
{
    /// <summary>
    /// Represents the (JSON) data for resources as stored in /system/resources/_all.json.
    /// </summary>
    public class ResourcesData
    {
        [JsonProperty("files")]
        public string[] StaticContentItemUrls { get; set; }
    }
}
