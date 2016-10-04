using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    public class Tsi1946TestEntity : EntityModel
    {
        public string SingleLineText { get; set; }
        public string MultiLineText { get; set; }
        public RichText RichText { get; set; }
        public double Number { get; set; }
        public DateTime Date { get; set; }
        public Tag Keyword { get; set; }
        public Link CompLink { get; set; }
    }
}
