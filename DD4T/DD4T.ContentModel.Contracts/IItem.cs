using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel
{
    public interface IItem : IModel
    {
        string Id { get; }
        string Title { get; }
    }
}
