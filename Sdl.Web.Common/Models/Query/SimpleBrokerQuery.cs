namespace Sdl.Web.Common.Models
{
    public class SimpleBrokerQuery : Query
    {
        public int SchemaId { get; set; }
        public int PublicationId { get; set; }
        public string Sort { get; set; }
    }
}
