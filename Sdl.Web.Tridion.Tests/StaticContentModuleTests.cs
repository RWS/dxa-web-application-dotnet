using System;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Statics;


namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class StaticContentModuleTests 
    {
        [TestMethod]
        public void SetResponse_BinaryModified_XPMEnabled()
        {
            //Arrange
            var response = GetMockHttpResponse();
            var ifModifiedSince = Convert.ToDateTime("Fri, 22 Sep 2017 09:53:46 GMT");
            var lastModified = ifModifiedSince.AddDays(1);
            var localization = new Localization { IsXpmEnabled = true };
            
            //Act
            StaticContentModule.SetResponseProperties(response.Object, lastModified, ifModifiedSince, "", localization, false);

            //Assert
            response.Verify(r=>r.Cache.SetExpires(It.IsAny<DateTime>()) ,Times.Never);
            response.Verify(r => r.Cache.SetCacheability(It.IsAny<HttpCacheability>()), Times.Never);
            response.Verify(r => r.Cache.SetMaxAge(It.IsAny<TimeSpan>()), Times.Never);
            response.Verify(r => r.Cache.SetLastModified(It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void SetResponse_BinaryModified_XPMDisabled()
        {
            //Arrange
            var response = GetMockHttpResponse();
            var ifModifiedSince = Convert.ToDateTime("Fri, 22 Sep 2017 09:53:46 GMT");
            var lastModified = ifModifiedSince.AddDays(1);
            var localization = new Localization { IsXpmEnabled = false };

            //Act
            StaticContentModule.SetResponseProperties(response.Object, lastModified, ifModifiedSince, "", localization, false);

            //Assert
            response.Verify(r => r.Cache.SetExpires(It.IsAny<DateTime>()), Times.Once);
            response.Verify(r => r.Cache.SetCacheability(It.IsAny<HttpCacheability>()), Times.Once);
            response.Verify(r => r.Cache.SetMaxAge(It.IsAny<TimeSpan>()), Times.Once);
            response.Verify(r => r.Cache.SetLastModified(It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void SetResponse_BinaryNOTModified()
        {
            //Arrange
            var response = GetMockHttpResponse();
            var ifModifiedSince = Convert.ToDateTime("Fri, 22 Sep 2017 09:53:46 GMT");
            var lastModified = ifModifiedSince;
            var localization = new Localization();

            //Act
            StaticContentModule.SetResponseProperties(response.Object, lastModified, ifModifiedSince, "", localization, false);

            //Assert
            Assert.AreEqual(response.Object.StatusCode , 304);
            Assert.AreEqual(response.Object.SuppressContent, true);
        }

        private Mock<HttpResponseBase> GetMockHttpResponse()
        {
            var response = new Mock<HttpResponseBase>();
            response.SetupAllProperties();
            response.Setup(r => r.Cache.SetLastModified(It.IsAny<DateTime>()));
            return response;
        }
    }
}
