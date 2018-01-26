using System.Collections.Generic;

namespace Sdl.Web.DataModel.Extension
{
    public interface ITargetGroup
    {
        string Id { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        IList<ICondition> Conditions { get; }
    }
}
