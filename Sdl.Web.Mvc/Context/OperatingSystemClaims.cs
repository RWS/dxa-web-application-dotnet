namespace Sdl.Web.Mvc.Context
{
    /// <summary>
    /// Represents the claims about the Operating System of the user's device.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </remarks>
    public class OperatingSystemClaims : ContextClaims
    {
        /// <summary>
        /// Gets the name of the "aspect" which the strongly typed claims class represents.
        /// </summary>
        /// <returns>The name of the aspect.</returns>
        protected internal override string GetAspectName()
        {
            return "os";
        }

        public string Model
        {
            get { return GetClaimValue<string>("model"); }
        }

        public string Variant
        {
            get { return GetClaimValue<string>("variant"); }
        }

        public string Vendor
        {
            get { return GetClaimValue<string>("vendor"); }
        }

        public string Version
        {
            get { return GetClaimValue<string>("version"); }
        }
    }
}
