namespace Sdl.Web.ModelService
{
    public interface IModelServiceResponse
    {
        uint Hashcode { get; }
    }

    public class ModelServiceResponse<T> : IModelServiceResponse
    {
        protected ModelServiceResponse(T response, uint hashcode)
        {
            Response = response;
            Hashcode = hashcode;
        }
        internal static ModelServiceResponse<T> Create(T response, uint hashcode) => new ModelServiceResponse<T>(response, hashcode);
        public T Response { get; private set; }
        public uint Hashcode { get; private set; }
    }
}
