using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion.Config
{
    /// <summary>
    /// Placeholder class to add publication resolving when this is available in the Tridion .NET API
    /// </summary>
    public class ServiceLocalizationResolver : ILocalizationResolver
    {
        public Localization GetLocalizationFromUri(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Localization GetLocalizationFromId(string localizationId)
        {
            throw new NotImplementedException();
        }
    }
}
