using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sdl.Web.Common.Logging;

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
    }
}
