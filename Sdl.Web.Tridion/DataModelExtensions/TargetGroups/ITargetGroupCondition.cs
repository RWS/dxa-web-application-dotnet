namespace Sdl.Web.DataModel.Extension
{
    public interface ITargetGroupCondition : ICondition
    {
        ITargetGroup TargetGroup { get; }
    }
}
