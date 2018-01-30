using System;
using System.Runtime.Serialization;

namespace Sdl.Web.ModelService.Request
{
    public enum ContentType
    {
        IGNORE,
        RAW, // RAW will perform no conversion and return what is in the Broker
        MODEL,
    }

    public enum DataModelType
    {
        R2,  // Return R2 data model
        DD4T // Return DD4T data model format
    }

    public interface IModelServiceRequest
    {
        /// <summary>
        /// Serialization binder used when deserializing the request from the model service. This can
        /// be used to map types during deserialization.
        /// </summary>
        SerializationBinder Binder { get; set; }

        /// <summary>
        /// Returns the request Uri for the given model service.
        /// </summary>
        /// <param name="modelService">Model service</param>
        /// <returns>Request Uri</returns>
        Uri BuildRequestUri(ModelServiceClient modelService);
    }
}
