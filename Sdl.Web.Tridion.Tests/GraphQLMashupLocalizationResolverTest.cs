using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Sdl.Web.Common.Configuration;

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
            Localization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            Localization resolvedLocalization = _testLocalizationResolver.GetLocalization($"tcm:0-{testLocalization.Id}-1");

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.Id, resolvedLocalization.Id, "resolvedLocalization.Id");
        }
    }
}
