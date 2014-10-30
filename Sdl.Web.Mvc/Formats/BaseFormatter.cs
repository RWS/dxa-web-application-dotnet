using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public interface IDataFormatter
    {
        double Score(ControllerContext controllerContext);
        ActionResult FormatData(ControllerContext controllerContext, object model);
        bool ProcessModel { get; }
    }

    /// <summary>
    /// Base class for formatting model in data format
    /// </summary>
    public abstract class BaseFormatter : IDataFormatter
    {
        private readonly List<string> _mediaTypes = new List<string>();

        public void AddMediaType(string mediaType)
        {
            _mediaTypes.Add(mediaType);
        }

        public abstract ActionResult FormatData(ControllerContext controllerContext, object model);

        public virtual double Score(ControllerContext controllerContext)
        {
            double score = 0.0;
            List<string> validTypes = DataFormatters.GetValidTypes(controllerContext,_mediaTypes);
            if (validTypes.Any())
            {
                foreach(var type in validTypes)
                {
                    var thisScore = DataFormatters.GetScoreFromAcceptString(type);
                    if (thisScore>score)
                    {
                        score = thisScore;
                    }
                }
            }
            return score;
        }

        public bool ProcessModel { get; protected set; }
    }
}
