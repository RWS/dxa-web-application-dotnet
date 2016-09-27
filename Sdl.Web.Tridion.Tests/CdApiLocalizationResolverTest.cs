using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;


namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class CdApiLocalizationResolverTest : TestClass
    {
        private const string TestBaseUrl = "http://localhost:8880";

        private static readonly ILocalizationResolver _testLocalizationResolver = new CdApiLocalizationResolver();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void ResolveLocalization_Existing_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            Localization resolvedLocalization =  _testLocalizationResolver.ResolveLocalization(testUrl);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.LocalizationId, resolvedLocalization.LocalizationId, "resolvedLocalization.LocalizationId");
            Assert.AreEqual(testLocalization.Path, resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreNotEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }


        [TestMethod]
        public void ResolveLocalization_EscapedChars_Success() // See CRQ-1585
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + TestFixture.Tsi1278PageUrlPath);

            Localization resolvedLocalization = _testLocalizationResolver.ResolveLocalization(testUrl);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.LocalizationId, resolvedLocalization.LocalizationId, "resolvedLocalization.LocalizationId");
            Assert.AreEqual(testLocalization.Path, resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreNotEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }

        [TestMethod]
        public void ResolveLocalization_NonExisting_Exception()
        {
            Uri testUrl = new Uri("http://nonexisting");

            AssertThrowsException<DxaUnknownLocalizationException>(() => _testLocalizationResolver.ResolveLocalization(testUrl));
        }

        [TestMethod]
        public void GetLocalization_Known_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            // Ensure that the test Localization is Known and initialized before starting the test
            _testLocalizationResolver.ResolveLocalization(testUrl);

            Localization resolvedLocalization = _testLocalizationResolver.GetLocalization(testLocalization.LocalizationId);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.LocalizationId, resolvedLocalization.LocalizationId, "resolvedLocalization.LocalizationId");
            Assert.AreEqual(testLocalization.Path, resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreNotEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }

        [TestMethod]
        public void GetLocalization_Unknown_Success()
        {
            string testLocalizationId = "666666"; 

            Localization resolvedLocalization = _testLocalizationResolver.GetLocalization(testLocalizationId);

            // ILocalizationResolver.GetLocalization on an unknown Localization ID should return a Localization instance with only LocalizationId set.
            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalizationId, resolvedLocalization.LocalizationId, "resolvedLocalization.LocalizationId");
            Assert.IsNull(resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }

    }
}
