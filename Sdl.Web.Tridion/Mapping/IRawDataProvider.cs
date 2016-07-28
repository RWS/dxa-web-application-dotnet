using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Interface used by <see cref="Sdl.Web.Tridion.Navigation.StaticNavigationProvider"/> to access Navigation.json
    /// </summary>
    public interface IRawDataProvider
    {
        string GetPageContent(string urlPath, Localization localization);
    }
}
