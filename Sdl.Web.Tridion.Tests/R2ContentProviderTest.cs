using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class R2ContentProviderTest : ContentProviderTest
    {
        public R2ContentProviderTest()
            : base(new R2Mapping.DefaultContentProvider(), () => TestFixture.R2TestLocalization)
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }
    }
}
