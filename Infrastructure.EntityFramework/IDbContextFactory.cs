using System.Data.Entity;

namespace Infrastructure.EntityFramework
{
    public interface IDbContextFactory
    {
        DbContext GetContext();
    }
}