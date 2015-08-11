using System;

namespace Sdl.Web.Common.Models.Common
{
    public class ExceptionEntity : EntityModel
    {
        public Exception Exception
        {
            get;
            private set;
        }

        public ExceptionEntity(Exception ex)
        {
            Exception = ex;
        }
    }
}
