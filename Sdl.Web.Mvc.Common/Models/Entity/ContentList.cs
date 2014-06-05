using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Models
{
    public class ContentList<T> : Entity
    {
        //TODO add concept of filtering/query (filter options and active filters/query)
        public string Headline { get; set; }
        public int PageSize { get; set; }
        public bool EnablePaging { get; set; }
        public Tag ContentType { get; set; }
        public int Start { get; set; }
        public List<T> ItemListElements { get; set; }
        public ContentList()
        {
            ItemListElements = new List<T>();
        }
    }
}
