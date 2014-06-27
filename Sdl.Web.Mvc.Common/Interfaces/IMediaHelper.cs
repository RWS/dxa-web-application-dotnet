using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface IMediaHelper
    {
        int GetResponsiveWidth(string widthFactor, int containerSize = 0);
        int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0);
        string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0);
    }
}
