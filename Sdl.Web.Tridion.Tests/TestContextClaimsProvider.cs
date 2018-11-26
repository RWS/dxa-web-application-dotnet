using System;
using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestContextClaimsProvider : IContextClaimsProvider
    {
        public IDictionary<string, object> GetContextClaims(string aspectName, Localization localization)
        {
            throw new NotImplementedException();
        }

        public string GetDeviceFamily()
        {
            throw new NotImplementedException();
        }
    }
}
