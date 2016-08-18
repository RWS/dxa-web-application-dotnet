using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public abstract class DynamicList : EntityModel
    {
        public DynamicList()
        {
            QueryResults = new List<EntityModel>();
        }

        public bool HasMore { get; set; }

        [JsonIgnore]
        public List<EntityModel> QueryResults
        {
            get;
            set;
        }
    
        public abstract Query GetQuery(Localization localization);

        public abstract Type ResultType { get; }
    }
}
