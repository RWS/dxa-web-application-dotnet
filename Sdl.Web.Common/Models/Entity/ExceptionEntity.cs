using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    [Serializable]
    public class ExceptionEntity : EntityModel
    {
        public string[] ErrorMessages { get; private set;}

        [JsonIgnore]
        public Exception Exception { get; private set; }

        public ExceptionEntity(Exception ex)
        {
            Exception = ex;

            IList<string> errorMessages = new List<string>();
            do
            {
                errorMessages.Add(ex.Message);
                ex = ex.InnerException;
            } while (ex != null);
            ErrorMessages = errorMessages.ToArray();

            MvcData = GetDefaultView(null);
        }

        public override MvcData GetDefaultView(Localization localization)
        {
            return new MvcData("EntityError");
        }
    }
}
