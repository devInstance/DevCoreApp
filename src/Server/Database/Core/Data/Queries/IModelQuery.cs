namespace DevInstance.SampleWebApp.Server.Database.Core.Data.Queries
{
    public interface IModelQuery<T, D>
    {
        T CreateNew();
        void Add(T record);
        void Update(T record);
        void Remove(T record);
        D Clone();
    }
}
