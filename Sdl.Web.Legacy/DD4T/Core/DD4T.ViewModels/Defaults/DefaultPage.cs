using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels.Attributes;
using DD4T.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ViewModels.Defaults
{
    [PageViewModel]
    public class DefaultPage : ViewModelBase
    {
        [PageTitle]
        public string ItemTitle { get; set; }

        [ComponentPresentations]
        public List<IRenderableViewModel> Items { get; set; }

    }
}
