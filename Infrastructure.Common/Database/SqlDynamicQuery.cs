using System;
using System.Data.Linq.Mapping;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Infrastructure.Common.Linq;

namespace Infrastructure.Common.Database
{
    public class SqlDynamicQuery<TEntity> : ExpressionVisitorBase<SqlDynamicQuery<TEntity>.QueryState>
    {
        private readonly StringBuilder _whereOut;
        private readonly Dictionary<string, int> _ids = new Dictionary<string, int>();
        private readonly ExpandoObject _parameters = new ExpandoObject();
        private readonly Dictionary<int, string> _paramsByHash = new Dictionary<int, string>();
        private readonly Type _targetType;
        private readonly string _tableName;

        private readonly Lazy<string> _wherePart;
        private readonly Lazy<string> _orderPart;
        private readonly Lazy<string> _selectPart;

        private Expression<Func<TEntity, bool>> _whereExpression;
        private SortOrders<TEntity> _sortOrders = SortOrders<TEntity>.Create();
        private bool _hasOrders;
        private int _skipCount;
        private int _takeCount;

        public string TableName
        {
            get { return _tableName; }
        }

        public SqlDynamicQuery()
        {
            _whereOut = new StringBuilder();
            _targetType = typeof(TEntity);
            _tableName = GetTableName(_targetType);

            _wherePart = new Lazy<string>(ConstructWhere);
            _orderPart = new Lazy<string>(ConstructOrderBy);
            _selectPart = new Lazy<string>(ConstructSelect);
        }


        /*Methods*/
        public QueryObject ToSql()
        {
            if (_skipCount > 0 && !_hasOrders)
                throw new Exception("Can't skip rows in unsorted result");

            var queryString = _skipCount == 0 ? ConstructQuery() : ConstructPagedQuery();

            return new QueryObject(queryString, _parameters);
        }

        public QueryObject CountQuery()
        {
            return new QueryObject(
                string.Format("SELECT COUNT(*) FROM [{0}] {1}", _tableName, _wherePart.Value),
                _parameters);
        }

        private string ConstructQuery()
        {
            return string.Format("SELECT{0} {1} FROM [{2}] {3} {4}",
                _takeCount > 0 ? string.Format(" TOP {0}", _takeCount) : string.Empty,
                _selectPart.Value,
                _tableName,
                _wherePart.Value,
                _orderPart.Value);

        }


        public SqlDynamicQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            _whereExpression = predicate;
            return this;
        }

        public SqlDynamicQuery<TEntity> Skip(int count)
        {
            _skipCount = count;
            return this;
        }

        public SqlDynamicQuery<TEntity> Take(int count)
        {
            _takeCount = count;
            return this;
        }

        public SqlDynamicQuery<TEntity> AddOrderBy<TKey>(Expression<Func<TEntity, TKey>> orderBy, SortDirection direction)
        {
            _hasOrders = true;
            _sortOrders.AddSortBy(orderBy, direction);
            return this;
        }

        public SqlDynamicQuery<TEntity> AddOrderBy<TKey>(OrderByItem<TEntity,TKey> order)
        {
            _hasOrders = true;
            _sortOrders.AddSortBy(order);
            return this;
        }

        public SqlDynamicQuery<TEntity> SetSorting(SortOrders<TEntity> sortOrders)
        {
            if (sortOrders != null && sortOrders.Orders.Count > 0)
                _hasOrders = true;

            _sortOrders = sortOrders;
            return this;
        }


        protected override void VisitBinary(Context context, BinaryExpression node)
        {
            if ((IsAnotherMember(node.Left) || IsConstant(node.Left)) && IsTargetMember(node.Right))
                node = SwapOperands(node);

            string oper;
            string operand = null;

            switch (node.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    oper = "AND";
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    oper = "OR";
                    break;
                case ExpressionType.GreaterThan:
                    oper = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    oper = ">=";
                    break;
                case ExpressionType.LessThan:
                    oper = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    oper = "<=";
                    break;
                case ExpressionType.Equal:
                    oper = "=";
                    break;
                case ExpressionType.NotEqual:
                    oper = "!=";
                    break;
                default:
                    throw new NotImplementedException();
            }

            Out('(');
            Visit(
                Context.Create(node,
                    new BinaryQueryState()
                    {
                        IsLeftOperand = true,
                        SiblingOperandType = node.Right.GetType(),
                        Operator = node.NodeType
                    }), node.Left);

            if (IsConstant(node.Right) || IsAnotherMember(node.Right))
            {
                object value = null;

                if (IsConstant(node.Right))
                {
                    value = ((ConstantExpression)node.Right).Value;
                    if (value != null)
                    {
                        var tempName = "Param";
                        var left = node.Left as MemberExpression;
                        if (left != null)
                        {
                            tempName = GetMemberName(left);
                        }

                        operand = "@" + AddParameter(tempName, value);
                    }
                }

                if (IsAnotherMember(node.Right))
                {
                    var right = (MemberExpression)node.Right;

                    value = GetMemberValue(right);
                    if (value != null && !(node.Left is BinaryExpression))
                    {
                        operand = "@" + GetMemberParamNameAndStoreValue(right, value);
                    }
                }

                if (value == null)
                {
                    operand = "null";
                    switch (node.NodeType)
                    {
                        case ExpressionType.Equal:
                            oper = "is";
                            break;
                        case ExpressionType.NotEqual:
                            oper = "is not";
                            break;
                    }
                }
            }

            Out(' ');
            Out(oper);
            Out(' ');

            if (string.IsNullOrEmpty(operand))
                Visit(
                    Context.Create(node,
                        new BinaryQueryState()
                        {
                            IsRightOperand = true,
                            SiblingOperandType = node.Left.GetType(),
                            Operator = node.NodeType
                        }), node.Right);
            else
            {
                Out(operand);
            }

            Out(')');

        }

        protected override void VisitMember(Context context, MemberExpression node)
        {
            if (!context.HasParent)
            {
                var binaryNode = Expression.MakeBinary(ExpressionType.Equal, node, Expression.Constant(true));
                VisitBinary(Context.Create(node, null), binaryNode);
                return;
            }

            var state = context.State as BinaryQueryState;
            if (state != null
                && node.Type == typeof(bool)
                && state.SiblingOperandType != typeof(ConstantExpression)
                && state.SiblingOperandType != typeof(MemberExpression))
            {
                var binaryNode = Expression.MakeBinary(ExpressionType.Equal, node, Expression.Constant(true));
                VisitBinary(Context.Create(node, null), binaryNode);
                return;
            }

            if (IsTargetMember(node))
            {
                Out(GetFullTargetMemberName(node));
            }
            else
            {
                var paramName = GetMemberParamNameAndStoreValue(node);
                Out('@');
                Out(paramName);
            }
        }

        protected override void VisitUnary(Context context, UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Not)
            {
                throw new NotSupportedException("Not supported type of unary operation");
            }

            var constantNode = Expression.Constant(true);
            var binaryNode = Expression.MakeBinary(ExpressionType.NotEqual, node.Operand, constantNode);

            VisitBinary(Context.Create(context.Parent, context.State), binaryNode);
        }

        protected override void VisitConstant(Context context, ConstantExpression node)
        {
            if (node.Value == null)
            {
                Out("null");
            }
            else
            {
                var parameterName = AddParameter("Param", node.Value);

                Out('@');
                Out(parameterName);
            }
        }

        protected override void VisitMethodCall(Context context, MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Contains":
                    ProccessStringContains(node);
                    break;
                case "StartsWith":
                    ProcessStartsWith(node);
                    break;
                case "EndsWith":
                    ProcessEndsWith(node);
                    break;
                default:
                    throw new NotImplementedException(string.Format("Method '{0}' not supported", node.Method.Name));

            }
        }

        private string ConstructPagedQuery()
        {
            return string.Format(@"
;WITH Result_CTE AS
(
SELECT {0}, ROW_NUMBER() OVER ({1}) AS RowNum FROM [{2}] {3}
)
SELECT {0} FROM Result_CTE WHERE RowNum > {4} AND RowNum < {5}
", _selectPart.Value, _orderPart.Value, _tableName, _wherePart.Value, _skipCount, _skipCount + _takeCount);
        }

        private string ConstructWhere()
        {
            if (_whereExpression == null)
                return string.Empty;

            Visit(_whereExpression.Body);
            return string.Format("WHERE {0}", _whereOut);
        }

        private string ConstructOrderBy()
        {
            if (!_hasOrders) return string.Empty;

            var tempOrderString = string.Join(", ",
                _sortOrders.Orders.Select(
                    x => string.Format("{0} {1}", GetFullTargetMemberName(((LambdaExpression)x.BodyExpression).Body as MemberExpression),
                        x.Direction == SortDirection.Descending ? "DESC" : "ASC")));

            return string.Format("ORDER BY {0}", tempOrderString);
        }

        private string ConstructSelect()
        {
            return string.Format("[{0}].*", _tableName);
        }

        private static string GetTableName(Type t)
        {
            var attribute = t.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();

            var tableName = attribute != null ? ((TableAttribute)attribute).Name : t.Name;

            return tableName;
        }

        private string GetTargetMemberName(MemberExpression node)
        {
            var attribute = node.Member.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

            var memberName = attribute != null ? ((ColumnAttribute)attribute).Name : node.Member.Name;

            return memberName;
        }

        private string GetFullTargetMemberName(MemberExpression node)
        {
            return string.Format("[{0}].[{1}]", _tableName, GetTargetMemberName(node));
        }

        private void ProccessStringContains(MethodCallExpression node)
        {
            if (!IsTargetMember(node.Object))
                Out('@');

            Out(GetMemberName((MemberExpression)node.Object));
            Out(" LIKE '%' + ");
            Visit(Context.Create(node, null), node.Arguments[0]);
            Out(" + '%'");
        }

        private void ProcessStartsWith(MethodCallExpression node)
        {
            if (!IsTargetMember(node.Object))
            {
                Out('@');
                Out(GetMemberName((MemberExpression)node.Object));
            }
            else
            {
                Out(GetFullTargetMemberName((MemberExpression)node.Object));
            }


            Out(" LIKE ");
            Visit(Context.Create(node, null), node.Arguments[0]);
            Out(" + '%'");
        }

        private void ProcessEndsWith(MethodCallExpression node)
        {
            if (!IsTargetMember(node.Object))
                Out('@');

            Out(GetMemberName((MemberExpression)node.Object));
            Out(" LIKE '%' + ");
            Visit(Context.Create(node, null), node.Arguments[0]);
        }

        private void Out(string s)
        {
            _whereOut.Append(s);
        }

        private void Out(char c)
        {
            _whereOut.Append(c);
        }

        private string AddParameter(string name, object value)
        {
            name = GetAndStoreParameterName(name);
            ((IDictionary<string, object>)_parameters).Add(name, value);

            return name;
        }

        private string GetMemberName(MemberExpression node)
        {
            return IsTargetMember(node) ? GetTargetMemberName(node) : node.Member.Name;
        }

        private string GetMemberParamNameAndStoreValue(MemberExpression node, object value = null)
        {
            var hash = node.Member.GetHashCode();
            if (_paramsByHash.ContainsKey(hash))
            {
                return _paramsByHash[hash];
            }

            var memberName = GetMemberName(node);
            var memberValue = value ?? GetMemberValue(node);

            memberName = AddParameter(memberName, memberValue);

            _paramsByHash.Add(hash, memberName);

            return memberName;
        }

        private string GetAndStoreParameterName(string parameter)
        {
            var id = 0;
            if (_ids.ContainsKey(parameter))
            {
                _ids[parameter]++;
                id = _ids[parameter];
            }
            else
            {
                _ids.Add(parameter, id);
            }

            return string.Format("{0}{1}", parameter, id);
        }

        private object GetMemberValue(MemberExpression node)
        {
            var objectMember = Expression.Convert(node, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            return getter();
        }

        private BinaryExpression SwapOperands(BinaryExpression node)
        {
            var nodeType = node.NodeType;

            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                    nodeType = ExpressionType.LessThan;
                    break;
                case ExpressionType.LessThan:
                    nodeType = ExpressionType.GreaterThan;
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    nodeType = ExpressionType.LessThanOrEqual;
                    break;
                case ExpressionType.LessThanOrEqual:
                    nodeType = ExpressionType.GreaterThanOrEqual;
                    break;
            }

            return Expression.MakeBinary(nodeType, node.Right, node.Left, node.IsLiftedToNull, node.Method);
        }

        private bool IsTargetMember(Expression expression)
        {
            return (expression is MemberExpression) && ((MemberExpression)expression).Member.DeclaringType == _targetType;
        }

        private bool IsAnotherMember(Expression expression)
        {
            return (expression is MemberExpression) && ((MemberExpression)expression).Member.DeclaringType != _targetType;
        }

        private bool IsConstant(Expression expression)
        {
            return expression is ConstantExpression;
        }

        public class QueryState
        {
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class BinaryQueryState : QueryState
        {
            public bool IsLeftOperand { get; set; }
            public bool IsRightOperand { get; set; }

            public Type SiblingOperandType { get; set; }
            public ExpressionType Operator { get; set; }
        }

        public class QueryObject
        {
            public QueryObject(string query, dynamic parameters)
            {
                Parameters = parameters;
                Query = query;
            }

            public string Query { get; private set; }
            public dynamic Parameters { get; private set; }
        }
    }
}