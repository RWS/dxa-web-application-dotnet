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
        protected internal override string GetAspectName() => "device";

        public int DisplayHeight => GetClaimValue<int>("displayHeight");

        public int DisplayWidth => GetClaimValue<int>("displayWidth");

        public bool IsMobile => GetClaimValue<bool>("mobile");

        public string Model => GetClaimValue<string>("model");

        public int PixelDensity => GetClaimValue<int>("pixelDensity");

        public double PixelRatio => GetClaimValue<double>("pixelRatio");

        public bool IsRobot => GetClaimValue<bool>("robot");

        public bool IsTablet => GetClaimValue<bool>("tablet");

        public string Variant => GetClaimValue<string>("variant");

        public string Vendor => GetClaimValue<string>("vendor");

        public string Version => GetClaimValue<string>("version");
    }
}
