using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    /// <summary>
    /// JSON ActionResult using JSON.NET serializer instead of the JavaScriptSerializer which is used by default in ASP.NET MVC. 
    /// </summary>
    /// <remarks>
    /// Based on code in this post: http://james.newtonking.com/archive/2008/10/16/asp-net-mvc-and-json-net
    /// </remarks>
    public class JsonNetResult : JsonResult
    {
        public JsonSerializerSettings SerializerSettings { get; set; }

        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = ContentType ?? "application/json";
            if (ContentEncoding != null)
            {
                response.ContentEncoding = ContentEncoding;
            }

            if (Data != null)
            {
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting };
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);
                writer.Flush();
            }
        }
    }
}
