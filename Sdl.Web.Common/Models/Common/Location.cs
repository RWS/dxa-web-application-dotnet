using System;

namespace Sdl.Web.Common.Models
{
    public class Location : EntityBase
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public String Query { get; set; } 
    }
}
