using Sdl.Web.Common.Models;
using System;

namespace Tri.Example.Products
{
    public class Product : EntityBase
    {
        //From CMS
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProductId { get; set; }

        //From Product System
        public double Price { get; set; }
        public int Stock { get; set; }
    }
}