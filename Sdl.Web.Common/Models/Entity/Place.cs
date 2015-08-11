namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = SchemaOrgVocabulary, EntityName = "Place", Prefix = "s", Public = true)]
    public class Place : EntityModel
    {
        [SemanticProperty("s:name")]
        public string Name { get; set; }
        [SemanticProperty("s:image")]
        public Image Image { get; set; }
        [SemanticProperty("s:address")]
        public string Address { get; set; }
        [SemanticProperty("s:telephone")]
        public string Telephone { get; set; }
        [SemanticProperty("s:faxNumber")]
        public string FaxNumber { get; set; }
        [SemanticProperty("s:email")]
        public string Email { get; set; }
        [SemanticProperty("s:geo")]
        public Location Location { get; set; }
    }
}
