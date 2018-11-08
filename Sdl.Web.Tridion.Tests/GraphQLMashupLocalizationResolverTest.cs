using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Interfaces;
using System;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class GraphQLMashupLocalizationResolverTest : LocalizationResolverTest  
    {
        public GraphQLMashupLocalizationResolverTest() : base(new GraphQLMashupLocalizationResolver())
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetLocalization_WithTcmPrefix_Success()
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            ILocalization resolvedLocalization = _testLocalizationResolver.GetLocalization($"tcm:0-{testLocalization.Id}-1");

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.Id, resolvedLocalization.Id, "resolvedLocalization.Id");
        }
    }
}
