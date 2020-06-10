namespace GitOut.Features.Storage
{
    public interface IWritableStorage
    {
        void Write(string key, object value);
    }
}
