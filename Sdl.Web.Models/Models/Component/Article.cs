using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Article : Entity
    {
        public string Headline { get; set; }
        public Image Image { get; set; }
        public string ArticleBody { get; set; }

        public DateTime Date { get; set; }
        public string Summary { get; set; }
        public string Intro { get; set; }
        public List<Paragraph> Paragraphs { get; set; }
        //TODO author, other meta
    }
}