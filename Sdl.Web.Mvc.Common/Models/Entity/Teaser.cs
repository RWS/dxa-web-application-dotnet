﻿using System;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity(EntityName = "Image", Prefix = "i", Vocab = CoreVocabulary)]
    [SemanticEntity(EntityName = "Article", Prefix = "a", Vocab = CoreVocabulary)]
    public class Teaser : Entity
    {
        //A teaser can be mapped from an article, in which case the link should be to the article itself
        [SemanticProperty("a:_self")]
        public Link Link { get; set; }
        [SemanticProperty("headline")]
        [SemanticProperty("subheading")]
        public string Headline { get; set; }
        //A teaser can be mapped from an individual image, in which case the image property is set from the source entity itself
        [SemanticProperty("i:_self")]
        [SemanticProperty("a:image")]
        public MediaItem Media { get; set; }
        [SemanticProperty("content")]
        [SemanticProperty("a:introText")]
        public string Text { get; set; }
        public DateTime? Date { get; set; }
        public Location Location { get; set; }
    }
}