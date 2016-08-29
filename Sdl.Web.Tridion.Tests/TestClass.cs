using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

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

        protected void OutputJson(object objectToSerialize)
        {
            string json = JsonConvert.SerializeObject(
                objectToSerialize, 
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
                );
            Console.WriteLine("---- JSON Representation of {0} ----", objectToSerialize.GetType().FullName);
            Console.WriteLine(json);
        }

        protected TException AssertThrowsException<TException>(Action action, string actionName = null)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Fail("Action {0} did not throw an exception. Expected exception {1}.", actionName, typeof(TException).Name);
                return null; // Should never get here
            }
            catch (TException ex)
            {
                Console.WriteLine("Expected exception was thrown by action {0}:", actionName);
                Console.WriteLine(ex.ToString());
                return ex;
            }
        }

        protected static void AssertValidLink(Link link, string urlPath, string linkText, string alternateText, string message)
        {
            Assert.AreEqual(urlPath, link.Url, message + ".Url");
            Assert.AreEqual(linkText, link.LinkText, message + ".LinkText");
            Assert.AreEqual(alternateText, link.AlternateText, message + ".AlternateText");
        }
    }
}
