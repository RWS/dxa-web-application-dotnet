using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Models
{
    public abstract class DynamicList : EntityModel
    {
        public DynamicList()
        {
            QueryResults = new List<EntityModel>();
        }

        public bool HasMore { get; set; }

        public List<EntityModel> QueryResults
        {
            get;
            set;
        }
    
        public abstract Sdl.Web.Common.Query GetQuery(Localization localization);

        public abstract Type ResultType { get; }
    }
}
