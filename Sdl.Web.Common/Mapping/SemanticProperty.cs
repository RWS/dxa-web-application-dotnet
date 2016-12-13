
namespace Sdl.Web.Common.Mapping
{
    public class SemanticProperty
    {
        public SemanticProperty(string name) : this(null, name) { }
        public SemanticProperty(string prefix, string name)
        {
            Prefix = prefix;
            PropertyName = name;
        }
        public string PropertyName { get; set; }
        public string Prefix { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Prefix, PropertyName);
        }
    }
}
