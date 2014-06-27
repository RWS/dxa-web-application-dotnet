using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models
{
    public class Location : EntityBase
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public String Query { get; set; } 
    }
}
