using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.ModelService;

namespace Sdl.Web.Tridion.Tests
{
    /// <summary>
    /// Unit/integration tests for the <see cref="DefaultContentProvider"/> (using "legacy" DXA Model Service) using R2 Data Model.
    /// </summary>
    [Ignore] // TODO: TSI-3926 - need to put back once model-service is working on stack
    public class DefaultContentProviderTest : ContentProviderTest
    {
        public DefaultContentProviderTest()
            : base(new DefaultContentProvider(), () => TestFixture.ParentLocalization)
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext, typeof(DefaultModelServiceProvider));
        }

    }
}
