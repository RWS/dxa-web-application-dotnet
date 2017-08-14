using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DD4T.ContentModel
{  
    public interface IBinaryMeta
    {
        string Id { get; }
        string VariantId { get; }
        DateTime LastPublishedDate { get; }
        bool HasLastPublishedDate { get; }
    }
}
