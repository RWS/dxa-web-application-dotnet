using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface ILocalizationManager
    {
        void SetLocalizations(List<Dictionary<string, string>> localizations);
        DateTime GetLastLocalizationRefresh(string localizationId);
        void UpdateLocalization(Localization loc, bool loadDetails = false); 
        Localization GetContextLocalization();
        Localization GetLocalizationFromUri(Uri uri);
        Localization GetLocalizationFromId(string localizationId);
    }
}
