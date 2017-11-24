namespace Sdl.Web.ModelService
{
    public interface IModelServiceResponse
    {
        uint Hashcode { get; }
    }

    /// <summary>
    /// Model Service Response
    /// </summary>
    public class ModelServiceResponse<T> : IModelServiceResponse
    {
        protected ModelServiceResponse(T response, uint hashcode)
        {
            Response = response;
            Hashcode = hashcode;
        }
        internal static ModelServiceResponse<T> Create(T response, uint hashcode) => new ModelServiceResponse<T>(response, hashcode);

        /// <summary>
        /// Deserialized Model Service response
        /// </summary>
        public T Response { get; private set; }

        /// <summary>
        /// Returns hash code of Model Service response
        /// </summary>
        public uint Hashcode { get; private set; }
    }
}
