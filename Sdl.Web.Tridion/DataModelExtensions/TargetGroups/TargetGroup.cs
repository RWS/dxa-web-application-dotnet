using System.Collections.Generic;

namespace Sdl.Web.DataModel.Extension
{
    public class TargetGroup
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
        
        public IList<Condition> Conditions { get; set; }
    }
}
