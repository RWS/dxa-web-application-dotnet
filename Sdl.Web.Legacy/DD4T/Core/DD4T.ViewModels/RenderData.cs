using DD4T.Core.Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ViewModels
{
    public class RenderData : IRenderData
    {
        public string View
        {
            get;
            set;
        }

        public string Controller
        {
            get;
            set;
        }

        public string Action
        {
            get;
            set;
        }

        public string Region
        {
            get;
            set;
        }
    }
}
