namespace Infrastructure.Domain
{
    public interface IRepository<TEntity> : IReadRepository<TEntity>,
        IWriteRepository<TEntity> where TEntity : IEntity
    {
    }
}
