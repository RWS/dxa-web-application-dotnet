using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Contracts.Serializing;

namespace DD4T.Serialization
{
    public class SerializationProperties : ISerializationProperties
    {
        public bool CompressionEnabled { get; set; }
    }
}
