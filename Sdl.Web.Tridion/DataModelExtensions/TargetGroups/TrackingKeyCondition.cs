namespace Sdl.Web.DataModel.Extension
{
    public class TrackingKeyCondition : Condition
    {
        public string TrackingKeyTitle { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
