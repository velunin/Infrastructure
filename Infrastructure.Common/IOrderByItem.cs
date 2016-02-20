using System;
using System.Linq;
using System.Linq.Expressions;

namespace Infrastructure.Common
{
    public interface IOrderByItem<TEntity>
    {
        Expression BodyExpression { get; }
        SortDirection Direction { get; set; }
        ParameterExpression ParameterExpression { get; }
        Type ReturnType { get; }

        IQueryable<TEntity> ApplyOrder(IQueryable<TEntity> collection, bool initial);
    }
}