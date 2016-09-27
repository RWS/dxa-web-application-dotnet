using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Context;


namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class ContextServiceClaimsProviderTest : TestClass
    {
        private static readonly ContextServiceClaimsProvider _testContextClaimsProvider = new ContextServiceClaimsProvider();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetContextClaims_All_Success()
        {
            _testContextClaimsProvider.DefaultUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36";

            IDictionary<string, object> contextClaims =  _testContextClaimsProvider.GetContextClaims(null);

            Assert.IsNotNull(contextClaims, "contextClaims");
            OutputJson(contextClaims);

            Assert.AreEqual(41, contextClaims.Count, "contextClaims.Count");
            Assert.AreEqual("Google", contextClaims["browser.vendor"], "contextClaims['browser.vendor']");
            Assert.AreEqual("Chrome", contextClaims["browser.model"], "contextClaims['browser.model']");
            Assert.AreEqual("Microsoft", contextClaims["os.vendor"], "contextClaims['os.vendor']");
            Assert.AreEqual("Windows", contextClaims["os.model"], "contextClaims['os.model']");
        }

        [TestMethod]
        public void GetContextClaims_BrowserOnly_Success()
        {
            _testContextClaimsProvider.DefaultUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36";

            IDictionary<string, object> contextClaims = _testContextClaimsProvider.GetContextClaims("browser");

            Assert.IsNotNull(contextClaims, "contextClaims");
            OutputJson(contextClaims);

            Assert.AreEqual(18, contextClaims.Count, "contextClaims.Count");
            Assert.AreEqual("Google", contextClaims["browser.vendor"], "contextClaims['browser.vendor']");
            Assert.AreEqual("Chrome", contextClaims["browser.model"], "contextClaims['browser.model']");
            Assert.IsTrue(contextClaims.All(cc => cc.Key.StartsWith("browser.")), "Non-browser claims found");
        }

        [TestMethod]
        public void GetDeviceFamily_Success()
        {
            string deviceFamily = _testContextClaimsProvider.GetDeviceFamily();
            Assert.IsNull(deviceFamily, "deviceFamily");
        }
    }
}
