namespace GitOut.Features.Storage
{
    public interface IStorage
    {
        T? Get<T>(string key) where T : class;
        void Set(string key, object value);
    }
}
