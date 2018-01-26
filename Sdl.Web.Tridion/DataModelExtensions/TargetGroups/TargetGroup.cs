using System.Collections.Generic;

namespace Sdl.Web.DataModel.Extension
{
    public class TargetGroup : ITargetGroup
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
        
        public IList<ICondition> Conditions { get; set; }
    }
}
