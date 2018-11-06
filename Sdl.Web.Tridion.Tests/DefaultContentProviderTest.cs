using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Tests
{
    /// <summary>
    /// Unit/integration tests for the <see cref="DefaultContentProvider"/> (using "legacy" DXA Model Service) using R2 Data Model.
    /// </summary>
    [TestClass]
    public class DefaultContentProviderTest : ContentProviderTest
    {
        public DefaultContentProviderTest()
            : base(new DefaultContentProvider(), () => TestFixture.ParentLocalization)
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }
    }
}
