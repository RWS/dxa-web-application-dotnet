using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion.Tests
{
    /// <summary>
    /// Abstract base class for all Test Classes.
    /// </summary>
    public abstract class TestClass
    {
        protected static void DefaultInitialize(TestContext testContext)
        {
            Log.Info("==== {0} ====", testContext.FullyQualifiedTestClassName);
            TestFixture.InitializeProviders();
        }
    }
}
