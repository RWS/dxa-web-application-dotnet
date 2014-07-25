using System;

namespace Sdl.Web.Common.Models.Common
{
    public class Location : EntityBase
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public String Query { get; set; } 
    }
}
