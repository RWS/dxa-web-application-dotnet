using System;
using System.Collections.Generic;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Common.Models.Entity
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName= "Article", Prefix= "s", Public=true)]
    public class Article : EntityBase
    {
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        public Image Image { get; set; }        
        [SemanticProperty("s:dateCreated")]
        public DateTime? Date { get; set; }        
        public string Description { get; set; }
        [SemanticProperty("s:articleBody")]
        public List<Paragraph> ArticleBody { get; set; }
    }
}