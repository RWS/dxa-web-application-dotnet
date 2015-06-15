using System;

namespace Sdl.Web.Common.Models
{
    [Obsolete("Deprecated in DXA 1.1. The Model Type should be determined using the ViewModel class hierarchy.")]
    public enum ModelType
    {
        Entity = 0, // Default
        Page,
        Region,
    }
}
