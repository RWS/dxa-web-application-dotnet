using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class LocalizationTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void Load_Success()
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;

            OutputJson(testLocalization);

            Assert.IsNotNull(testLocalization.Id, "testLocalization.Id");
            Assert.IsNotNull(testLocalization.Path, "testLocalization.Path");
            Assert.AreEqual("en-US", testLocalization.Culture, "testLocalization.Culture");
            Assert.IsNotNull(testLocalization.CultureInfo, "testLocalization.CultureInfo");
            Assert.AreEqual("AutoParentLang", testLocalization.Language, "testLocalization.Language");
            Assert.IsTrue(testLocalization.IsXpmEnabled, "testLocalization.IsXpmEnabled");
            Assert.IsTrue(testLocalization.IsHtmlDesignPublished, "testLocalization.IsHtmlDesignPublished");
            Assert.IsTrue(testLocalization.IsDefaultLocalization, "testLocalization.IsDefaultLocalization");
            StringAssert.Matches(testLocalization.Version, new Regex(@"^v\d+\.\d+$"));
            Assert.IsNotNull(testLocalization.DataFormats, "testLocalization.DataFormats");
            Assert.AreEqual(3, testLocalization.DataFormats.Count, "testLocalization.DataFormats.Count");
            Assert.IsNotNull(testLocalization.SiteLocalizations, "testLocalization.SiteLocalizations");
            Assert.AreEqual(2, testLocalization.SiteLocalizations.Count, "testLocalization.SiteLocalizations.Count");
            Assert.AreNotEqual(DateTime.MinValue, testLocalization.LastRefresh, "testLocalization.LastRefresh");

            foreach (ILocalization siteLocalization in testLocalization.SiteLocalizations)
            {
                // Check that Site Localizations are "pre-initialized".
                Assert.IsNotNull(siteLocalization.Id, "siteLocalization.Id");
                Assert.IsNotNull(siteLocalization.Path, "siteLocalization.Path");
                Assert.IsNotNull(siteLocalization.Language, "siteLocalization.Language");
            }
            Assert.IsTrue(testLocalization.SiteLocalizations.Contains(testLocalization), "testLocalization.SiteLocalizations.Contains(testLocalization)");
        }

    }
}
