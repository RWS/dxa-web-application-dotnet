namespace DD4T.ContentModel
{
    public interface ISchema : IRepositoryLocal
    {
        IOrganizationalItem Folder { get; }
        string RootElementName { get; }
    }
}
