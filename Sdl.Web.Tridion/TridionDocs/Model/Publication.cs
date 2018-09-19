using System;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.TridionDocs.Model
{
    /// <summary>
    /// Publication
    /// </summary>
    public class Publication
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> ProductFamily { get; set; }
        public List<string> ProductReleaseVersion { get; set; }
        public string VersionRef { get; set; }
        public string Language { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Version { get; set; }
        public string LogicalId { get; set; }
    }
}
