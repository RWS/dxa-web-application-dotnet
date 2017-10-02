using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ContentModel.Contracts.Caching
{
    public interface IMessageProvider
    {
        void Start();
    }
    public interface IMessageProvider<T> : IMessageProvider, IObservable<T> where T : IEvent
    {
    }
}
