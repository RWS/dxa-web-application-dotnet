using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface ILocalizationResolver
    {
        Localization GetLocalizationFromUri(Uri uri);
        Localization GetLocalizationFromId(string localizationId);
    }
}
