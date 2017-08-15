using DD4T.ContentModel.Contracts.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Utils.Caching
{
    public class CacheEvent : ICacheEvent
    {
        public string RegionPath { get; set; }
        public string Key { get; set; }
        public int Type { get; set; }
    }
}
