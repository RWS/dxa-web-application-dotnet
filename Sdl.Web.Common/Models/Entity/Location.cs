using System;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = SchemaOrgVocabulary, EntityName = "GeoCoordinates", Prefix = "s", Public = true)]
    public class Location : EntityModel
    {
        [SemanticProperty("s:longitude")]
        public double Longitude { get; set; }
        [SemanticProperty("s:latitude")]
        public double Latitude { get; set; }
        public String Query { get; set; } 
    }
}
