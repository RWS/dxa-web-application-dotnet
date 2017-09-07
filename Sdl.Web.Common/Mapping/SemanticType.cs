namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents a Semantic Type.
    /// </summary>
    public class SemanticType
    {
        public string EntityName { get; }
        public string Vocab { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="vocab"></param>
        public SemanticType(string entityName, string vocab)
        {
            EntityName = entityName;
            Vocab = vocab;
        }

        public override string ToString()
            => $"{Vocab}:{EntityName}";
    }
}
