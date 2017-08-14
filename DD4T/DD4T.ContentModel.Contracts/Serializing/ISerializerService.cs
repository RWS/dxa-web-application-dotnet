
namespace DD4T.ContentModel.Contracts.Serializing
{
    public interface ISerializerService
    {
        string Serialize<T>(T input) where T : IModel;

        T Deserialize<T>(string input) where T : IModel;

        bool IsAvailable();
    }
}
