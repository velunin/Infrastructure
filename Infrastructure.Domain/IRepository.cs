namespace Infrastructure.Domain
{
    public interface IRepository<TEntity,TKey> : IReadRepository<TEntity,TKey>,
        IWriteRepository<TEntity, TKey> where TEntity : IEntity<TKey> where TKey : struct
    {
    }
}
