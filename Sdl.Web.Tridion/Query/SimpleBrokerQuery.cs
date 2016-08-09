using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion.Query
{
    public class SimpleBrokerQuery : Sdl.Web.Common.Query
    {
        public int SchemaId { get; set; }
        public int PublicationId { get; set; }
        public string Sort { get; set; }
    }
}
