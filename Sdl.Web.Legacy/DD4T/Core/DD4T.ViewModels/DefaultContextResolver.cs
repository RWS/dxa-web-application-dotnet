using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultContextResolver : IContextResolver
    {
        private readonly IContextModel _contextModel;
        public DefaultContextResolver()
        {
            _contextModel = new ContextModel();
        }
        public IContextModel ResolveContextModel(IModel modelData)
        {
            BuildContextModel(modelData);
            return _contextModel;
        }

        private void BuildContextModel(IModel modelData)
        {
            if (modelData == null)
                return;

            if (modelData is IPage)
            {
                _contextModel.PageId = new TcmUri(((IPage)modelData).Id);
            }
        }
    }
}
