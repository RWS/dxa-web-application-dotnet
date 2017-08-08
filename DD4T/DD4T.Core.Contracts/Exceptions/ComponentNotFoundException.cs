using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Exceptions
{
    [Serializable]
    [Obsolete("Use ComponentPresentationException instead")]
    public class ComponentNotFoundException : ApplicationException
    {
    }
}
