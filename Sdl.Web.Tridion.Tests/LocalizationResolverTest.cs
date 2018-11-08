using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;


namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public abstract class LocalizationResolverTest : TestClass
    {
        protected const string TestBaseUrl = "http://dxatest.ams.dev:98";

        protected readonly ILocalizationResolver _testLocalizationResolver = new GraphQLLocalizationResolver();

        protected LocalizationResolverTest(ILocalizationResolver testLocalizationResolver)
        {
            _testLocalizationResolver = testLocalizationResolver;
        }

        [TestMethod]
        public void ResolveLocalization_Existing_Success()
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            ILocalization resolvedLocalization =  _testLocalizationResolver.ResolveLocalization(testUrl);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.Id, resolvedLocalization.Id, "resolvedLocalization.Id");
            Assert.AreEqual(testLocalization.Path, resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreNotEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }


        [TestMethod]
        public void ResolveLocalization_EscapedChars_Success() // See CRQ-1585
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;
            string testPageUrlPath = testLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1278PageRelativeUrlPath);

            Uri testUrl = new Uri(TestBaseUrl + testPageUrlPath);

            ILocalization resolvedLocalization = _testLocalizationResolver.ResolveLocalization(testUrl);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.Id, resolvedLocalization.Id, "resolvedLocalization.Id");
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
            ILocalization testLocalization = TestFixture.ParentLocalization;
            Uri testUrl = new Uri(TestBaseUrl + testLocalization.Path);

            // Ensure that the test Localization is Known and initialized before starting the test
            _testLocalizationResolver.ResolveLocalization(testUrl);

            ILocalization resolvedLocalization = _testLocalizationResolver.GetLocalization(testLocalization.Id);

            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalization.Id, resolvedLocalization.Id, "resolvedLocalization.Id");
            Assert.AreEqual(testLocalization.Path, resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreNotEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }

        [TestMethod]
        public void GetLocalization_Unknown_Success()
        {
            string testLocalizationId = "666666";

            ILocalization resolvedLocalization = _testLocalizationResolver.GetLocalization(testLocalizationId);

            // ILocalizationResolver.GetLocalization on an unknown Localization ID should return a Localization instance with only LocalizationId set.
            Assert.IsNotNull(resolvedLocalization, "resolvedLocalization");
            OutputJson(resolvedLocalization);

            Assert.AreEqual(testLocalizationId, resolvedLocalization.Id, "resolvedLocalization.Id");
            Assert.IsNull(resolvedLocalization.Path, "resolvedLocalization.Path");
            Assert.AreEqual(DateTime.MinValue, resolvedLocalization.LastRefresh, "resolvedLocalization.LastRefresh");
        }

    }
}
