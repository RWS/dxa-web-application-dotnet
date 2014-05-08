
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity("http://www.sdl.com/tridion/schemas/core", "Article")]
    [SemanticEntity("http://schema.org", "Article", "s")]
    public class Article : Entity
    {
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        public Image Image { get; set; }        
        [SemanticProperty("s:dateCreated")]
        public DateTime? Date { get; set; }        
        public string Description { get; set; }
        [SemanticProperty("s:articleBody")]
        public List<Paragraph> Paragraphs { get; set; }
    }
}