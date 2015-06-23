namespace Sdl.Web.Mvc.Context
{
    /// <summary>
    /// Represents claims about the user's device.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </remarks>
    public class DeviceClaims : ContextClaims
    {
        /// <summary>
        /// Gets the name of the "aspect" which the strongly typed claims class represents.
        /// </summary>
        /// <returns>The name of the aspect.</returns>
        protected internal override string GetAspectName()
        {
            return "device";
        }

        public int DisplayHeight
        {
            get { return GetClaimValue<int>("displayHeight"); }
        }

        public int DisplayWidth
        {
            get { return GetClaimValue<int>("displayWidth"); }
        }

        public bool IsMobile
        {
            get { return GetClaimValue<bool>("mobile"); }
        }

        public string Model
        {
            get { return GetClaimValue<string>("model"); }
        }

        public int PixelDensity
        {
            get { return GetClaimValue<int>("pixelDensity"); }
        }

        public double PixelRatio
        {
            get { return GetClaimValue<double>("pixelRatio"); }
        }

        public bool IsRobot
        {
            get { return GetClaimValue<bool>("robot"); }
        }

        public bool IsTablet
        {
            get { return GetClaimValue<bool>("tablet"); }
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
