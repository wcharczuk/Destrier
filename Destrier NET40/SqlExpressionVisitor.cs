using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Destrier
{
    /// <summary>
    /// Class to read a lambda and turn it into sql predicate syntax.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// given: LHS OP RHS, if both the LHS and RHS are of type T, the LHS will turn into a column reference and RHS will be evaluated to a constant.
    /// </remarks>
    public class SqlExpressionVisitor<T>
    {
        public SqlExpressionVisitor()
        {
            this.Buffer = new StringBuilder();
            this.Parameters = new Dictionary<string, object>();
            this.Type = typeof(T);
            this.Members = Model.GenerateAllMembers(this.Type).ToDictionary(m => m.FullyQualifiedName);
        }

        public SqlExpressionVisitor(StringBuilder buffer) : this()
        {
            this.Buffer = buffer;
        }

        public SqlExpressionVisitor(Dictionary<string, object> parameters) : this()
        {
            this.Parameters = parameters;
        }

        public SqlExpressionVisitor(Dictionary<String, Member> members)
        {
            this.Type = typeof(T);
            this.Buffer = new StringBuilder();
            this.Parameters = new Dictionary<string, object>();
            this.Members = members;
        }

        public SqlExpressionVisitor(StringBuilder buffer, Dictionary<string, object> parameters) : this()
        {
            this.Buffer = buffer;
            this.Parameters = parameters;
        }

        public SqlExpressionVisitor(StringBuilder buffer, IDictionary<string, object> parameters, Dictionary<String, Member> members)
        {
            this.Type = typeof(T);
            this.Buffer = buffer;
            this.Parameters = parameters;
            this.Members = members;
        }

        public Type Type { get; set; }
        public StringBuilder Buffer { get; set; }
        public IDictionary<String, object> Parameters { get; set; }
        public Dictionary<String, Member> Members { get; set; }

        public void Visit(Expression expression, ExpressionType? parentNodeType = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    VisitBinaryExpression(expression);
                    break;
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    VisitBinaryExpression(expression);
                    break;
                case ExpressionType.MemberAccess:
                    VisitMemberAccess(expression, parentNodeType);
                    break;
                case ExpressionType.Call:
                    VisitCallExpression(expression);
                    break;
                case ExpressionType.Constant:
                    VisitConstantExpression(expression);
                    break;
                case ExpressionType.Convert:
                    VisitConvertExpression(expression);
                    break;
                case ExpressionType.Lambda:
                    VisitLambdaExpression(expression);
                    break;
                case ExpressionType.Not:
                    VisitUnaryExpression(expression);
                    break;
                default:
                    throw new Exception("Unsupported Operation: " + expression.NodeType.ToString());
            }
        }

        public Expression Reduce(Expression expression)
        {
            if (expression == null)
                return null;

            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return ReduceBinaryExpression(expression as BinaryExpression);
                case ExpressionType.MemberAccess:
                    return ReduceMemberAccessExpression(expression as MemberExpression);
                case ExpressionType.Call:
                    return ReduceCallExpression(expression as MethodCallExpression);
                case ExpressionType.Constant:
                    return ReduceConstantExpression(expression as ConstantExpression);
                case ExpressionType.TypeIs:
                    return ReduceTypeIsExpression(expression as TypeBinaryExpression);
                case ExpressionType.Convert:
                    return ReduceUnaryExpression(expression as UnaryExpression);
                case ExpressionType.Parameter:
                    return ReduceParameterExpression(expression as ParameterExpression);
                case ExpressionType.Lambda:
                    return ReduceLambdaExpression(expression as LambdaExpression);
                case ExpressionType.Not:
                    return ReduceUnaryExpression(expression as UnaryExpression);
                default:
                    return expression; //not reduceable ...
            }
        }

        protected void VisitBinaryExpression(Expression expression)
        {
            var left = ((BinaryExpression)expression).Left;
            var right = ((BinaryExpression)expression).Right;

            if(left.Type != this.Type)
                left = Reduce(left);

            if (right.Type != this.Type)
                right = Reduce(right);

            var rightIsNull = false;

            if (right.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.Constant)
            {
                var value = Evaluate(right);
                rightIsNull = value == null;
            }

            Visit(left, expression.NodeType);
            Buffer.AppendFormat(" {0} ", GetOperator(expression.NodeType, rightIsNull));
            Visit(right, expression.NodeType);
        }

        protected void VisitUnaryExpression(Expression expression)
        {
            var unaryExp = expression as UnaryExpression;
            if(unaryExp != null)
            {
                var operand = Reduce(unaryExp.Operand);

                Buffer.Append(GetUnaryOperator(expression.NodeType));
                Buffer.Append(" (");
                Visit(operand);
                Buffer.Append(") ");
            }
        }

        protected void VisitMemberAccess(Expression expression, ExpressionType? parentNodeType = null)
        {
            var memberExp = (MemberExpression)expression;
            var memberType = memberExp.Member.ReflectedType;

            Member member = null;
            var rootType = ReflectionHelper.RootTypeForExpression(memberExp);

            if (rootType != null && rootType.Equals(this.Type))
            {
                member = Model.MemberForExpression(memberExp, this.Members) as ColumnMember;

                if (member == null)
                    throw new Exception("Invalid Column.");

                WriteColumn(member as ColumnMember);

                var isDirectBooleanAccess = memberExp.Type.Equals(typeof(Boolean))
                    && (parentNodeType == null || (parentNodeType != null && parentNodeType.Value != ExpressionType.Equal && parentNodeType.Value != ExpressionType.NotEqual));

                if (isDirectBooleanAccess)
                {
                    Buffer.Append(" = 1");
                }
            }
            else
            {
                var reduced = Reduce(memberExp);
                WriteParameter(Evaluate(reduced));
            }
        }

        protected void VisitConstantExpression(Expression expression)
        {
            var constExp = expression as ConstantExpression;
            if(constExp != null)
            {
                var value = constExp.Value;
                if (value == null)
                {
                    Buffer.Append("null");
                }
                else if(constExp.Type.Equals(typeof(Boolean)))
                {
                    WriteLiteralValue(value);
                }
                else
                    WriteParameter(value);
            }
        }

        protected void VisitCallExpression(Expression expression, ExpressionType? parentNodeType = null)
        {
            var m = (MethodCallExpression)expression;
            bool evaluateCall = true;

            if (Members != null && Members.Any() && m.Object is MemberExpression)
            {
                var memberExp = m.Object as MemberExpression;
                var rootType = ReflectionHelper.RootTypeForExpression(memberExp);

                var like = "LIKE";
                var concat = " + ";

                if (rootType != null && rootType.Equals(this.Type))
                {
                    if (m.Method.DeclaringType == typeof(string))
                    {
                        switch (m.Method.Name)
                        {
                            case "StartsWith":
                                Buffer.Append("(");
                                this.Visit(m.Object);
                                Buffer.Append(String.Format(" {0} ", like));
                                this.Visit(m.Arguments[0]);
                                Buffer.Append(String.Format(" {0} '%')", concat));
                                return;
                            case "EndsWith":
                                Buffer.Append("(");
                                this.Visit(m.Object);
                                Buffer.Append(String.Format(" {0} '%' {1} ", like, concat));
                                this.Visit(m.Arguments[0]);
                                Buffer.Append(")");
                                return;
                            case "Contains":
                                Buffer.Append("(");
                                this.Visit(m.Object);
                                Buffer.Append(String.Format(" {0} '%' {1} ", like, concat));
                                this.Visit(m.Arguments[0]);
                                Buffer.Append(String.Format(" {0} '%')", concat));
                                return;
                            case "ToUpper":
                                Buffer.Append("UPPER(");
                                this.Visit(m.Object);
                                Buffer.Append(")");
                                return;
                            case "ToLower":
                                Buffer.Append("LOWER(");
                                this.Visit(m.Object);
                                Buffer.Append(")");
                                return;
                            case "Replace":
                                Buffer.Append("REPLACE(");
                                this.Visit(m.Object);
                                Buffer.Append(", ");
                                this.Visit(m.Arguments[0]);
                                Buffer.Append(", ");
                                this.Visit(m.Arguments[1]);
                                Buffer.Append(")");
                                return;
                            case "Trim":
                                Buffer.Append("RTRIM(LTRIM(");
                                this.Visit(m.Object);
                                Buffer.Append("))");
                                return;
                        }
                    }
                }
                else
                {
                    if (m.Arguments.Any())
                    {
                        var argumentType = ReflectionHelper.RootTypeForExpression(m.Arguments[0]);
                        if (ReflectionHelper.HasInterface(m.Method.DeclaringType, typeof(System.Collections.IList)) && argumentType.Equals(this.Type))
                        {
                            evaluateCall = false;
                            switch (m.Method.Name)
                            {
                                case "Contains":
                                    Visit(m.Arguments[0]);
                                    Buffer.Append(" IN ");
                                    ListToSet(Evaluate(Reduce(m.Object)) as System.Collections.IList);
                                    break;
                            }
                        }
                    }
                }
            }

            if (evaluateCall)
            {
                var method = Expression.Call(Reduce(m.Object), m.Method, ReduceArgumentList(m.Arguments)); //rebuilds this into a new 
                var result = Evaluate(method);

                var isDirectBooleanAccess = result is Boolean
                        && (parentNodeType == null || (parentNodeType != null && parentNodeType.Value != ExpressionType.Equal && parentNodeType.Value != ExpressionType.NotEqual));

                if (isDirectBooleanAccess)
                    WriteLiteralValue(result);
                else
                    WriteParameter(Evaluate(method));
            }
        }

        protected void ListToSet(System.Collections.IList list)
        {
            Buffer.Append("(");

            int index = 0;
            foreach (var obj in list)
            {
                if (obj is String)
                {
                    if (index > 0)
                        Buffer.Append(",");

                    WriteParameter(obj.ToString());
                }
                else if (obj is DateTime)
                {
                    if (index > 0)
                        Buffer.Append(",");

                    Buffer.Append("'" + obj.ToString() + "'");
                }
                else if (obj is ValueType)
                {
                    if (index > 0)
                        Buffer.Append(",");

                    if (obj.GetType().IsEnum)
                        Buffer.Append(((int)obj).ToString());
                    else
                        Buffer.Append(obj.ToString());
                }
                index++;
            }

            Buffer.Append(")");
        }

        protected void VisitConvertExpression(Expression expression)
        {
            var uExp = (UnaryExpression)expression;

            if (uExp.Operand.NodeType == ExpressionType.MemberAccess)
            {
                Visit(uExp.Operand);
            }
            else
            {
                var reduced = Reduce(uExp.Operand);
                var value = Evaluate(reduced);
                if (value == null)
                    Buffer.Append("null");
                else
                    WriteParameter(value);
            }
        }

        protected void VisitLambdaExpression(Expression expression)
        {
            var lex = expression as LambdaExpression;
            var reduced = Reduce(lex.Body);
            Visit(reduced);
        }

        protected Expression ReduceBinaryExpression(BinaryExpression expression)
        {
            var left = Reduce(expression.Left);
            var right = Reduce(expression.Right);
            var conversion = Reduce(expression.Conversion);
            return this.UpdateBinary(expression, left, right, conversion, expression.IsLiftedToNull, expression.Method);
        }

        protected BinaryExpression UpdateBinary(BinaryExpression b, Expression left, Expression right, Expression conversion, bool isLiftedToNull, MethodInfo method)
        {
            if (left != b.Left || right != b.Right || conversion != b.Conversion || method != b.Method || isLiftedToNull != b.IsLiftedToNull)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                {
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                }
                else
                {
                    return Expression.MakeBinary(b.NodeType, left, right, isLiftedToNull, method);
                }
            }
            return b;
        }

        protected Expression ReduceCallExpression(MethodCallExpression m)
        {
            Expression obj = this.Reduce(m.Object);
            IEnumerable<Expression> args = this.ReduceArgumentList(m.Arguments);

            return this.UpdateMethodCall(m, obj, m.Method, args);
        }

        protected MethodCallExpression UpdateMethodCall(MethodCallExpression m, Expression obj, MethodInfo method, IEnumerable<Expression> args)
        {
            if (obj != m.Object || method != m.Method || args != m.Arguments)
            {
                return Expression.Call(obj, method, args);
            }
            return m;
        }

        protected virtual Expression ReduceMemberAccessExpression(MemberExpression m)
        {
            Expression exp = this.Reduce(m.Expression);
            return this.UpdateMemberAccess(m, exp, m.Member);
        }

        protected MemberExpression UpdateMemberAccess(MemberExpression m, Expression expression, MemberInfo member)
        {
            if (expression != m.Expression || member != m.Member)
            {
                return Expression.MakeMemberAccess(expression, member);
            }
            return m;
        }

        protected virtual Expression ReduceTypeIsExpression(TypeBinaryExpression b)
        {
            Expression expr = this.Reduce(b.Expression);
            return this.UpdateTypeIs(b, expr, b.TypeOperand);
        }

        protected TypeBinaryExpression UpdateTypeIs(TypeBinaryExpression b, Expression expression, Type typeOperand)
        {
            if (expression != b.Expression || typeOperand != b.TypeOperand)
            {
                return Expression.TypeIs(expression, typeOperand);
            }
            return b;
        }

        protected virtual Expression ReduceConstantExpression(ConstantExpression c)
        {
            return c;
        }

        protected virtual Expression ReduceParameterExpression(ParameterExpression p)
        {
            return p;
        }

        protected virtual Expression ReduceUnaryExpression(UnaryExpression u)
        {
            Expression operand = this.Reduce(u.Operand);
            return this.UpdateUnary(u, operand, u.Type, u.Method);
        }

        protected UnaryExpression UpdateUnary(UnaryExpression u, Expression operand, Type resultType, MethodInfo method)
        {
            if (u.Operand != operand || u.Type != resultType || u.Method != method)
            {
                return Expression.MakeUnary(u.NodeType, operand, resultType, method);
            }
            return u;
        }

        protected virtual Expression ReduceLambdaExpression(LambdaExpression lambda)
        {
            Expression body = this.Reduce(lambda.Body);
            return this.UpdateLambda(lambda, lambda.Type, body, lambda.Parameters);
        }

        protected LambdaExpression UpdateLambda(LambdaExpression lambda, Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters)
        {
            if (body != lambda.Body || parameters != lambda.Parameters || delegateType != lambda.Type)
            {
                return Expression.Lambda(delegateType, body, parameters);
            }
            return lambda;
        }

        protected Expression[] ReduceArgumentList(ReadOnlyCollection<Expression> argumentList)
        {
            List<Expression> resolvedExpressions = new List<Expression>();
            foreach (var exp in argumentList)
            {
                switch (exp.NodeType)
                {
                    case ExpressionType.MemberAccess:
                    case ExpressionType.Constant:
                        resolvedExpressions.Add(exp);
                        break;
                }
            }
            return resolvedExpressions.ToArray();
        }

        private void WriteLiteralValue(object literal)
        {
            if(literal is bool)
            {
                if((Boolean)literal)
                    Buffer.Append("(1 = 1)");
                else
                    Buffer.Append("(1 = 0)");
            }
        }

        private void WriteName(String name)
        {
            Buffer.AppendFormat("{0}", name);
        }

        private void WriteColumn(ColumnMember column)
        {
            if (!String.IsNullOrEmpty(column.TableAlias))
            {
                Buffer.AppendFormat("{0}.{1}", WrapName(column.TableAlias, true), WrapName(column.Name));
            }
            else
            {
                Buffer.AppendFormat("{0}", WrapName(column.Name));
            }
        }

        private void WriteParameter(object value)
        {
            var paramName = System.Guid.NewGuid();
            Buffer.AppendFormat("@{0}", paramName.ToString("N"));
            Parameters.Add(paramName.ToString("N"), value);
        }

        private String WrapName(String name, Boolean isTableAlias = false)
        {
            return String.Format("[{0}]", name);
        }

        private object Evaluate(Expression e)
        {
            Type type = e.Type;

            if (e.NodeType == ExpressionType.Convert)
            {
                var u = (UnaryExpression)e;
                if (ReflectionHelper.IsNullableType(u.Operand.Type) && ReflectionHelper.GetUnderlyingTypeForNullable(u.Operand.Type) == ReflectionHelper.GetUnderlyingTypeForNullable(type))
                {
                    e = ((UnaryExpression)e).Operand;
                }
            }

            if (e.NodeType == ExpressionType.Constant)
            {
                var ce = (ConstantExpression)e;
                if (e.Type != type && ReflectionHelper.GetUnderlyingTypeForNullable(e.Type) == ReflectionHelper.GetUnderlyingTypeForNullable(type))
                {
                    e = ce = Expression.Constant(ce.Value, type);
                }
            }

            var me = e as MemberExpression;
            if (me != null)
            {
                var ce = me.Expression as ConstantExpression;
                if (ce != null)
                {
                    //need to do the evaluation now
                    return me.Member.GetValue(ce.Value);
                }
            }

            var constExp = e as ConstantExpression;
            if (constExp != null)
            {
                return constExp.Value;
            }
            
            //otherwise it's a funciton call and we need to build up an invocation...
            if (type.IsValueType)
            {
                e = Expression.Convert(e, typeof(object));
            }
            
            Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(e);
            Func<object> fn = lambda.Compile();
            return fn();
        }

        protected string GetUnaryOperator(ExpressionType nodeType)
        {
            string op = null;
            switch (nodeType)
            {
                case ExpressionType.Not:
                    op = "NOT";
                    break;
            }

            return op;
        }

        protected String GetOperator(ExpressionType nodeType, Boolean isNull = false)
        {
            string op = null;
            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                    op = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    op = ">=";
                    break;
                case ExpressionType.LessThan:
                    op = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    op = "<=";
                    break;
                case ExpressionType.Equal:
                    if (isNull)
                        op = "is";
                    else
                        op = "=";
                    break;
                case ExpressionType.NotEqual:
                    if (isNull)
                        op = "is not";
                    else
                        op = "<>";
                    break;
                case ExpressionType.AndAlso:
                    op = "and";
                    break;
                case ExpressionType.OrElse:
                    op = "or";
                    break;
                default:
                    throw new Exception("Operator not supported.");
            }
            return op;
        }
    }
}
