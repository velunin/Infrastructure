namespace Infrastructure.Domain
{
    public interface IEntity<TKey> where TKey: struct
    {
        TKey Id { get; } 
    }
}