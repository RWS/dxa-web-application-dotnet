namespace Sdl.Web.DataModel.Extension
{
    public class TargetGroupCondition : Condition, ITargetGroupCondition
    {
        public TargetGroup TargetGroup { get; set; }

        ITargetGroup ITargetGroupCondition.TargetGroup => TargetGroup as ITargetGroup;
    }
}
