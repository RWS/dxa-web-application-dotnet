using System;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    public  abstract class EclItem : MediaItem
    {
        /// <summary>
        /// ECL URI for External Content Library Components (null for normal multimedia Components)
        /// </summary>
        public string EclUri
        {
            // TODO: read ECL URI from published data (currently only available for ECL items embedded in RTF, so lets use the filename for now)
            get
            {
                // build ECL URI from filename (filename: 8-mm-204-dist-file.ecl ECL URI: ecl:8-mm-204-dist-file)
                return String.Format("ecl:{0}", FileName.Replace(".ecl", String.Empty));
            }
        }

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <remarks>
        /// ECL items will use ECL URI rather than TCM URI in XPM markup
        /// </remarks>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(Localization localization)
        {
            // replace TCM URI with ECL URI
            return base.GetXpmMarkup(localization).Replace(String.Format("tcm:{0}-{1}", localization.LocalizationId, Id), EclUri);
        }

        // TODO: provide default implementation of ToHtml using the ECL Template Fragment
        //public override string ToHtml(string widthFactor, double aspect = 0, string cssClass = null, int containerSize = 0)

        //public override void ReadFromXhtmlElement(XmlElement xhtmlElement)
        //{
        //    base.ReadFromXhtmlElement(xhtmlElement);
        //    EclUri = xhtmlElement.GetAttribute("eclUri");
        //}
    }
}
