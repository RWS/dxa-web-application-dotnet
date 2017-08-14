using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DD4T.ContentModel
{
    public interface IBinary : IComponent
    {
        byte[] BinaryData { get; set; }
        Stream BinaryStream { get; set; }
        string VariantId { get; }
        //IMultimedia Multimedia { get; set; }
        string Url { get; set;  }
    }
}
