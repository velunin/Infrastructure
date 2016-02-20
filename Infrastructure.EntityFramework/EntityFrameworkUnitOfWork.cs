using System;
using System.Data;
using System.Data.Entity;
using Infrastructure.Domain;

namespace Infrastructure.EntityFramework
{
    public class EntityFrameworkUnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private DbContextTransaction _transaction;

        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        public EntityFrameworkUnitOfWork(IDbContextFactory contextFactory)
        {
            _context = contextFactory.GetContext();
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int Commit()
        {
            int result;
             
            try
            {
                _transaction = _context.Database.BeginTransaction(IsolationLevel);
                result = _context.SaveChanges();
                _transaction.Commit();
            }
            catch (Exception)
            {
                _transaction.Rollback();
                throw;
            }

            return result;
        }
    }
}