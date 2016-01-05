using Newtonsoft.Json;

namespace Sdl.Web.Common.Models.Data
{
    /// <summary>
    /// Represents the (JSON) data for versioning as stored in /version.json.
    /// </summary>
    public class VersionData
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
