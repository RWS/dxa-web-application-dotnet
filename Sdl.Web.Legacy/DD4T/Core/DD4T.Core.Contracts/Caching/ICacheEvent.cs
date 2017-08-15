using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ContentModel.Contracts.Caching
{
    public interface ICacheEvent : IEvent
    {
        string RegionPath { get; set; }
        string Key { get; set; }
        int Type { get; set; }
    }
}
