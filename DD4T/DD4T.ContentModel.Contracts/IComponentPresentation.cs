using System;
using System.Collections.Generic;

namespace DD4T.ContentModel
{
    public interface IComponentPresentation : IModel
    {
        IComponent Component { get; }
        IComponentTemplate ComponentTemplate { get; }
        IPage Page { get; set; }
        bool IsDynamic { get; set; }
        string RenderedContent { get; }
        int OrderOnPage { get; set; }
        [Obsolete("Conditions is deprecated, please refactor your code to work with TargetGroup.Conditions from items within the TargetGroupConditions property")]
        IList<ICondition> Conditions { get; } 
        IList<ITargetGroupCondition> TargetGroupConditions { get; }
    }
}
