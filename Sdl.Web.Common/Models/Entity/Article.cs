using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName= "Article", Prefix= "s", Public=true)]
    public class Article : EntityBase
    {
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        [SemanticProperty("s:image")]
        public Image Image { get; set; }        
        [SemanticProperty("s:dateCreated")]
        public DateTime? Date { get; set; }
        [SemanticProperty("s:about")]
        public string Description { get; set; }
        [SemanticProperty("s:articleBody")]
        public List<Paragraph> ArticleBody { get; set; }
    }
}