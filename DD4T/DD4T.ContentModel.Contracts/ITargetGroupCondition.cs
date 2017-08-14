namespace DD4T.ContentModel
{
    public interface ITargetGroupCondition : ICondition
    {
        ITargetGroup TargetGroup { get; }
    }
}
