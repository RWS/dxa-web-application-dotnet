using Sdl.Web.Common.Utils;

namespace Sdl.Web.Common.Models
{
    public class SimpleBrokerQuery : Query
    {
        public int SchemaId { get; set; }
        public int PublicationId { get; set; }
        public string Sort { get; set; }

        public override int GetHashCode() => Hash.CombineHashCodes(
            SchemaId.GetHashCode(), PublicationId.GetHashCode(), Sort.GetHashCode(),
            MaxResults.GetHashCode(), PageSize.GetHashCode(), Start.GetHashCode(), Cursor == null ? 0 : Cursor.GetHashCode());
    }
}
