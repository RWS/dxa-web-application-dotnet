using System.Collections.Generic;

namespace DD4T.ContentModel
{
    public interface ITargetGroup : IRepositoryLocal
    {
        string Description { get; set; }
        IList<ICondition> Conditions { get; }
    }
}