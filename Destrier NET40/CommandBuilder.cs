using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Destrier.Extensions;

namespace Destrier
{
    public class CommandBuilderFactory
    {
        public static CommandBuilder<T> GetCommandBuilder<T>()
        {
            var connectionName = Model.ConnectionName(typeof(T));
            var provider = DatabaseConfigurationContext.GetProviderForConnection(connectionName);

            if (provider is Npgsql.NpgsqlFactory)
                return new PostgresCommandBuilder<T>();
            else
                return new SqlServerCommandBuilder<T>();
        }
    }

    public abstract class CommandBuilder<T>
    {
        public CommandBuilder()
        {
            _t = typeof(T);
            AsRootMember = ModelCache.GetRootMemberForType(_t);
            _d = SqlDialectVariantFactory.GetSqlDialect(_t);
            Initialize();
        }

        protected void Initialize()
        {
            this._orderByClause = new List<OrderByElement>();
            this.Parameters = new Dictionary<String, Object>();
            this.FullyQualifiedTableName = Model.TableNameFullyQualified(_t);

            this.Command = new StringBuilder();
            this.Parameters = new Dictionary<String, Object>();

            DiscoverMembers();
            SetupTableAliases(this.Members.Values, _tableAliases);
        }

        protected Type _t = null;
        protected ISqlDialectVariant _d = null;
        protected Dictionary<String, String> _tableAliases = new Dictionary<String, String>();

        public RootMember AsRootMember { get; set; }
        public Dictionary<String, Member> Members { get; set; }
        public ISqlDialectVariant Dialect { get { return _d; } }

        public String FullyQualifiedTableName { get; private set; }
        public String OutputTableName { get { return this.AsRootMember.OutputTableName; } }
        public String TableAlias { get { return this.AsRootMember.TableAlias; } }
        public Boolean UseNoLock { get { return this.AsRootMember.UseNoLock; } }

        public StringBuilder Command { get; set; }
        public IDictionary<String, Object> Parameters { get; set; }

        protected Expression<Func<T, bool>> _whereClause = null;
        protected dynamic _whereParameters = null;

        #region Select
        public Int32? Limit { get; set; }
        public Int32? Offset { get; set; }

        protected List<OrderByElement> _orderByClause = null;
        protected List<ChildCollectionMember> _includedChildCollections = new List<ChildCollectionMember>();
        protected List<Member> _outputMembers = new List<Member>();

        public Boolean HasChildCollections { get { return _includedChildCollections.Any(); } }
        public IEnumerable<ChildCollectionMember> ChildCollections { get { return _includedChildCollections.OrderByDescending(cm => ModelCache.HasChildCollectionMembers(cm.CollectionType)); } }
        
        public class OrderByElement
        {
            public Member Member { get; set; }
            public Boolean Ascending { get; set; }
        }

        #endregion

        #region Update
        protected List<String> _updateSets = new List<String>();
        #endregion

        public void AddWhere(Expression<Func<T, Boolean>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            this._whereClause = expression;
        }

        public void AddWhereDynamic(dynamic parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            _whereParameters = parameters;
        }

        #region Select Publics
        public void AddOrderBy<F>(Expression<Func<T, F>> expression)
        {
            var body = (MemberExpression)expression.Body;

            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause.Add(new OrderByElement() { Member = columnMember, Ascending = true });//String.Format("[{0}].[{1}] ASC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddOrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            var body = (MemberExpression)expression.Body;

            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause.Add(new OrderByElement() { Member = columnMember, Ascending = false });
        }

        public void AddThenOrderBy<F>(Expression<Func<T, F>> expression)
        {
            if (!_orderByClause.Any())
                throw new Exception("Need to run OrderBy or OrderByDescending first!");

            var body = (MemberExpression)expression.Body;
            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause.Add(new OrderByElement() { Member = columnMember, Ascending = true });
        }

        public void AddThenOrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            if (!_orderByClause.Any())
                throw new Exception("Need to run OrderBy or OrderByDescending first!");

            var body = (MemberExpression)expression.Body;
            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause.Add(new OrderByElement() { Member = columnMember, Ascending = false });
            //_orderByClause = _orderByClause + String.Format(", [{0}].[{1}] DESC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddIncludedChildCollection<F>(Expression<Func<T, F>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var body = (MemberExpression)expression.Body;
            var member = Model.MemberForExpression(body, Members) as ChildCollectionMember;

            if (member != null && !member.IsLazy)
                if (!_includedChildCollections.Any(ic => ic.Equals(member)))
                    _includedChildCollections.Add(member);
        }

        public void AddIncludedChildCollection(String fullyQualifiedMemberName)
        {
            if (Members.ContainsKey(fullyQualifiedMemberName))
            {
                var member = Members[fullyQualifiedMemberName] as ChildCollectionMember;
                if (member != null && !member.IsLazy)
                    if (!_includedChildCollections.Any(ic => ic.Equals(member)))
                        _includedChildCollections.Add(member);
            }
            else throw new Exception("Member not found!");
        }

        public void RemoveChildCollection<F>(Expression<Func<T, F>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var body = (MemberExpression)expression.Body;
            var childCollectionMember = Model.MemberForExpression(body, Members) as ChildCollectionMember;

            if (childCollectionMember == null)
                throw new Exception("Invalid child collection member!");

            _includedChildCollections.Remove(childCollectionMember);
        }

        public void RemoveChildCollection(String fullyQualifiedMemberName)
        {
            if (Members.ContainsKey(fullyQualifiedMemberName))
            {
                var member = Members[fullyQualifiedMemberName] as ChildCollectionMember;
                if (member != null)
                    _includedChildCollections.Remove(member);
            }
            else throw new Exception("Member not found!");
        }

        public void AddIncludeAll()
        {
            foreach (var member in Members.Values.Where(m => m is ChildCollectionMember && !m.IsLazy))
            {
                if (!_includedChildCollections.Any(ic => ic.Equals(member)))
                    _includedChildCollections.Add(member as ChildCollectionMember);
            }
        }

        public void RemoveAllChildCollections()
        {
            _includedChildCollections.Clear();
        }
        #endregion

        #region Update Publics
        public virtual void AddSet<F>(Expression<Func<T, F>> expression, Object value)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var memberExp = expression.Body as MemberExpression;

            if (memberExp == null)
                throw new ArgumentNullException("expression");

            var member = Model.MemberForExpression(memberExp, Members);

            var paramName = System.Guid.NewGuid();
            string param = String.Format("@{0}", paramName.ToString("N"));
            Parameters.Add(paramName.ToString("N"), value);

            _updateSets.Add(String.Format("{0} = {1}", member.Name, param));
        }

        public virtual void AddSet(String propertyName, Object value)
        {
            var member = Model.MemberForPropertyName(propertyName, Members);
            var paramName = System.Guid.NewGuid();
            string param = String.Format("@{0}", paramName.ToString("N"));
            Parameters.Add(paramName.ToString("N"), value);
            _updateSets.Add(String.Format("{0} = {1}", member.Name, param));
        }
        #endregion

        private void DiscoverMembers()
        {
            this.Members = new Dictionary<String, Member>();
            var memberList = ModelCache.GenerateMembersRecursive(_t); //ReflectionCache.MembersRecursiveCached(_t).ToList(); //this causes thread-safety issues. like an anti-boss.

            foreach (var m in memberList)
            {
                Members.Add(m.FullyQualifiedName, m);

                if (m is ColumnMember && !m.ParentAny(p => p is ChildCollectionMember))
                    _outputMembers.Add(m);

                var col_m = m as ChildCollectionMember;
                if (
                    col_m != null
                    && col_m.AlwaysInclude
                    && !col_m.ParentAny(p => p is ChildCollectionMember && !((ChildCollectionMember)p).AlwaysInclude)
                    && !col_m.IsLazy
                )
                {
                    _includedChildCollections.Add(col_m);
                }
            }
        }

        #region Select Internals

        public String AliasedParentColumnName(ChildCollectionMember cm, Boolean isInChildSection = false)
        {
            var parentAlias = String.Empty;
            if (cm.Parent != null)
                parentAlias = cm.Parent.TableAlias;
            else if (cm.Root != null)
                parentAlias = cm.Root.TableAlias;

            return String.Format("{0}.{1}", _d.WrapName(parentAlias, isGeneratedAlias: true), _d.WrapName(cm.ParentReferencedColumnName, isGeneratedAlias: isInChildSection));
        }

        public String AliasedColumnName(ChildCollectionMember cm)
        {
            return String.Format("{0}.{1}", _d.WrapName(cm.TableAlias, isGeneratedAlias: true), _d.WrapName(cm.ReferencedColumnName, isGeneratedAlias: false));
        }

        public String AliasedParentColumnName(ReferencedObjectMember m)
        {
            var parentAlias = String.Empty;
            if (m.Parent != null)
            {
                parentAlias = m.Parent.TableAlias;
            }
            else if (m.Root != null)
            {
                parentAlias = m.Root.TableAlias;
            }
            return String.Format("{0}.{1}", _d.WrapName(parentAlias, isGeneratedAlias: true), _d.WrapName(m.ReferencedColumnMember.Name, isGeneratedAlias: false));
        }

        public String AliasedColumnName(ReferencedObjectMember m)
        {
            var pks = Model.ColumnsPrimaryKey(m.Type);
            var pk = pks.FirstOrDefault();
            if (pk == null)
            {
                throw new Exception("No Primary Key to join on.");
            }
            return String.Format("{0}.{1}", _d.WrapName(m.TableAlias, isGeneratedAlias: true), _d.WrapName(pk.Name, isGeneratedAlias: false));
        }

        protected List<String> GetOutputColumns(IEnumerable<Member> members)
        {
            var outputColumns = new List<String>();

            foreach (var m in members)
            {
                outputColumns.Add(String.Format("{0}.{1} as {2}"
                    , _d.WrapName(m.TableAlias, isGeneratedAlias: true)
                    , _d.WrapName(m.Name, isGeneratedAlias: false)
                    , _d.WrapName(m.FullyQualifiedName, isGeneratedAlias: true)
                    ));
            }
            return outputColumns;
        }

        protected List<String> GetOutputPrimaryKeyMembers(IEnumerable<Member> members)
        {
            var outputColumns = new List<String>();

            foreach (var m in members.Select(m => m as ColumnMember).Where(cm => cm != null && cm.IsPrimaryKey && cm.Parent == null))
            {
                outputColumns.Add(String.Format("{0}.{1} asc"
                    , _d.WrapName(m.TableAlias, isGeneratedAlias: true)
                    , _d.WrapName(m.Name, isGeneratedAlias: false)
                    ));
            }
            return outputColumns;
        }

        protected String GetOrderByClause(String tableAlias = null, Boolean useTableQualifier = true, Boolean useFullyQualifiedName = false)
        {
            if (useTableQualifier)
            {
                var values = _orderByClause.Select(o => String.Format("{0}.{1} {2}"
                    , _d.WrapName(tableAlias ?? ((ColumnMember)o.Member).TableAlias, isGeneratedAlias: true)
                    , _d.WrapName(useFullyQualifiedName ? ((ColumnMember)o.Member).FullyQualifiedName : ((ColumnMember)o.Member).Name, isGeneratedAlias: !useFullyQualifiedName)
                    , o.Ascending ? "ASC" : "DESC")
                    );
                return string.Join(",", values);
            }
            else
            {
                var values = _orderByClause.Select(o => String.Format("{0} {1}"
                    , _d.WrapName(useFullyQualifiedName ? ((ColumnMember)o.Member).FullyQualifiedName : ((ColumnMember)o.Member).Name, isGeneratedAlias: !useFullyQualifiedName)
                    , o.Ascending ? "ASC" : "DESC")
                    );
                return string.Join(",", values);
            }
        }

        protected void SetupTableAliases(IEnumerable<Member> members, Dictionary<String, String> tableAliases)
        {
            foreach (ReferencedObjectMember refm in members.Where(m => m is ReferencedObjectMember && !m.IsLazy))
            {
                if (!tableAliases.ContainsKey(refm.FullyQualifiedName))
                {
                    tableAliases.Add(refm.FullyQualifiedName, Model.GenerateAlias());
                }
                refm.TableAlias = tableAliases[refm.FullyQualifiedName];
            }
            foreach (ChildCollectionMember cm in members.Where(m => m is ChildCollectionMember && !m.IsLazy))
            {
                if (!tableAliases.ContainsKey(cm.FullyQualifiedName))
                {
                    tableAliases.Add(cm.FullyQualifiedName, Model.GenerateAlias());
                }
                cm.TableAlias = tableAliases[cm.FullyQualifiedName];

                if (ModelCache.HasChildCollectionMembers(cm.CollectionType))
                {
                    cm.OutputTableName = Model.GenerateAlias();
                }
            }
            foreach (ColumnMember cm in members.Where(m => m is ColumnMember))
            {
                if (cm.Parent != null && !cm.Parent.IsLazy) 
                {
                    if (cm.Parent is ChildCollectionMember)
                    {
                        cm.TableAlias = tableAliases[cm.Parent.FullyQualifiedName];
                    }
                    else if (cm.Parent is ReferencedObjectMember)
                    {
                        cm.TableAlias = tableAliases[cm.Parent.FullyQualifiedName];
                    }
                }
                else
                {
                    cm.TableAlias = cm.Root.TableAlias;
                }
            }
        }

        protected void AddJoins(Type t, List<Member> members, StringBuilder command)
        {
            if (ModelCache.HasReferencedObjectMembers(t))
            {
                foreach (var rom in members.Where(m => m is ReferencedObjectMember && !m.IsLazy &&!m.ParentAny(rm => rm is ChildCollectionMember)).Select(m => m as ReferencedObjectMember))
                {
                    command.AppendFormat("\n\t{0} JOIN {1} {2} {5} on {3} = {4}",
                        rom.JoinType, //0
                        rom.FullyQualifiedTableName, //1
                        _d.WrapName(rom.TableAlias, isGeneratedAlias: true), //2
                        AliasedParentColumnName(rom),
                        AliasedColumnName(rom),
                        rom.UseNoLock ? _d.NOLOCK : String.Empty
                    );
                }
            }
        }

        protected void ProcessChildCollections()
        {
            List<String> tablesToDrop = new List<String>();
            foreach (var cm in ChildCollections)
            {
                var subMembers = ModelCache.GenerateMembersRecursive(cm.CollectionType, new RootMember(cm));

                var tableAliases = new Dictionary<String, String>();
                SetupTableAliases(subMembers, tableAliases);

                var outputColumns = GetOutputColumns(subMembers.Where(m => m is ColumnMember 
                    && !m.ParentAny(p => p is ChildCollectionMember || p.IsLazy))
                );

                var subColumnList = String.Join("\n\t, ", outputColumns);
                Command.AppendFormat("\n\nSELECT\n\t{0}", subColumnList);

                if (ModelCache.HasChildCollectionMembers(cm.CollectionType))
                {
                    Command.AppendFormat("\nINTO {0}", _d.TempTablePrefix(cm.OutputTableName));
                    tablesToDrop.Add(cm.OutputTableName);
                }

                Command.AppendFormat("\nFROM");

                var root = this.AsRootMember;
                var parent = cm.Parent ?? root;
                Command.AppendFormat("\n\t{0} {1}", _d.TempTablePrefix(parent.OutputTableName ?? root.OutputTableName), _d.WrapName(parent.TableAlias, isGeneratedAlias: true));

                Command.AppendFormat("\n\tINNER JOIN {1} {2} {5} ON {3} = {4}"
                    , cm.JoinType //0
                    , cm.FullyQualifiedTableName //1
                    , _d.WrapName(cm.TableAlias, isGeneratedAlias: true) //2
                    , AliasedParentColumnName(cm, isInChildSection:true)
                    , AliasedColumnName(cm)
                    , cm.UseNoLock ? _d.NOLOCK : String.Empty);
                AddJoins(cm.CollectionType, subMembers, Command);

                Command.Append(";");

                if (ModelCache.HasChildCollectionMembers(cm.CollectionType))
                {
                    Command.AppendFormat("\n\nSELECT * FROM {0};", _d.TempTablePrefix(cm.OutputTableName));
                }
            }

            Command.Append("\n");

            foreach (var table in tablesToDrop)
            {
                Command.AppendFormat("\nDROP TABLE {0};", _d.TempTablePrefix(table));
            }
        }

        #endregion

        public abstract String GenerateSelect();

        public virtual String GenerateUpdate()
        {
            Command = new StringBuilder();
            Command.Append("UPDATE\n");
            Command.AppendFormat("\t{0}", _d.WrapName(this.TableAlias, isGeneratedAlias: true));
            Command.Append("\nSET\n");

            Command.Append(String.Format("\t{0}", String.Join(",", _updateSets)));
            Command.Append("\nFROM");
            Command.AppendFormat("\n\t{0} {1}", this.FullyQualifiedTableName, _d.WrapName(this.TableAlias, isGeneratedAlias: true));
            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                SqlExpressionVisitor<T> visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Dialect = _d;
                visitor.Visit(_whereClause);
            }

            return Command.ToString();
        }
    }

    public class SqlServerCommandBuilder<T> : CommandBuilder<T>
    {
        public SqlServerCommandBuilder() : base() {}
        
        public override String GenerateSelect()
        {
            Command = new StringBuilder();
            var columnList = String.Join("\n\t, ", GetOutputColumns(_outputMembers));

            if (Offset != null)
            {
                if (Limit != null)
                    Command.AppendFormat("SELECT TOP {0} * ", Limit.Value);
                else
                    Command.AppendFormat("SELECT * ");

                if (_includedChildCollections.Any())
                {
                    Command.AppendFormat("\nINTO #{0}", this.OutputTableName);
                }

                Command.AppendFormat("\nFROM \n(\n");
            }

            if (Limit != null && Offset == null)
            {
                Command.AppendFormat("SELECT TOP {1} \n\t{0}", columnList, Limit.Value);
            }
            else
            {
                Command.AppendFormat("SELECT\n\t{0}", columnList);
            }

            if (Offset != null)
            {
                Command.Append("\n\t, ROW_NUMBER()");
                if (_orderByClause.Any())
                {
                    Command.AppendFormat(" OVER (order by {0})", GetOrderByClause());
                }
                else
                {
                    var primaryKeys = GetOutputPrimaryKeyMembers(_outputMembers);
                    var _primaryKeyOrderBy = String.Join(",", primaryKeys);
                    Command.AppendFormat(" OVER (order by {0})", _primaryKeyOrderBy);
                }
                Command.Append(" as [row_number_for_offset]");
            }

            if (_includedChildCollections.Any() && Offset == null)
            {
                Command.AppendFormat("\nINTO #{0}", this.OutputTableName);
            }
            Command.Append("\nFROM");

            Command.AppendFormat("\n\t{0} [{1}] {2}", this.FullyQualifiedTableName, this.TableAlias, this.UseNoLock ? "(NOLOCK)" : String.Empty);
            AddJoins(_t, Members.Values.ToList(), Command);

            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                SqlExpressionVisitor<T> visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Dialect = _d;
                visitor.Visit(_whereClause);
            }
            else if (_whereParameters != null)
            {
                Command.Append("\nWHERE\n\t1=1");
                if (_whereParameters is ValueType)
                {
                    var primaryKey = Members.Values.FirstOrDefault(m => m is ColumnMember && ((ColumnMember)m).IsPrimaryKey) as ColumnMember;
                    if(primaryKey != null)
                    {
                        var parameterName = System.Guid.NewGuid().ToString("N");
                        Command.AppendLine(String.Format("\n\tAND [{0}].[{1}] = @{2}", primaryKey.TableAlias, primaryKey.FullyQualifiedName, parameterName));
                        Parameters.Add(parameterName, ((Object)_whereParameters).DBNullCoalese());
                    }
                    else
                    {
                        throw new InvalidOperationException("Trying to select an object by id without a primary key");
                    }
                }
                else
                {
                    foreach (KeyValuePair<String, Object> kvp in Execute.Utility.DecomposeObject(_whereParameters))
                    {
                        if (Members.ContainsKey(kvp.Key))
                        {
                            var member = Members[kvp.Key];
                            var paramName = System.Guid.NewGuid();
                            Parameters.Add(paramName.ToString("N"), kvp.Value);
                            Command.AppendFormat("\n\tAND [{0}].[{1}] = @{2}", member.TableAlias, member.Name, paramName.ToString("N"));
                        }
                        else
                        {
                            var paramName = System.Guid.NewGuid();
                            Parameters.Add(paramName.ToString("N"), kvp.Value);
                            Command.AppendFormat("\n\tAND [{0}] = @{1}", kvp.Key, paramName.ToString("N"));
                        }
                    }
                }
            }

            if (Offset == null && _orderByClause.Any())
            {
                Command.Append("\nORDER BY");
                Command.AppendFormat("\n\t{0}", GetOrderByClause());
            }

            if (Offset != null)
            {
                Command.Append("\n) as [data]");
                Command.AppendFormat("\nWHERE [row_number_for_offset] > {0}", Offset.Value.ToString());
            }

            if (Offset != null && _orderByClause.Any())
            {
                Command.Append("\nORDER BY");
                Command.AppendFormat("\n\t{0}", GetOrderByClause(tableAlias: "data", useFullyQualifiedName:true));
            }

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\n\nSELECT * FROM #{0}", this.OutputTableName);

                if (_orderByClause.Any())
                {
                    Command.Append("\nORDER BY");
                    Command.AppendFormat("\n\t{0}", GetOrderByClause(useTableQualifier:false, useFullyQualifiedName:true));
                }

                ProcessChildCollections();
                Command.AppendFormat("\nDROP TABLE #{0}", this.OutputTableName);
            }

            return Command.ToString();
        }
    }

    public class PostgresCommandBuilder<T> : CommandBuilder<T>
    {
        public PostgresCommandBuilder() : base() {}
        
        public override String GenerateUpdate()
        {
            Command = new StringBuilder();
            Command.Append("UPDATE\n");
            Command.AppendFormat("\t{0} AS {1}", _d.WrapName(this.FullyQualifiedTableName, isGeneratedAlias: false), _d.WrapName(this.TableAlias, isGeneratedAlias: true));
            Command.Append("\nSET\n");

            Command.Append(String.Format("\t{0}", String.Join(",", _updateSets)));
            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                SqlExpressionVisitor<T> visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Dialect = _d;
                visitor.Visit(_whereClause);
            }

            return Command.ToString();
        }

        public override string GenerateSelect()
        {
 	        Command = new StringBuilder();
            var columnList = String.Join("\n\t, ", GetOutputColumns(_outputMembers));

            Command.AppendFormat("SELECT\n\t{0}", columnList);

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\nINTO {0}", _d.TempTablePrefix(this.OutputTableName));
            }

            Command.AppendFormat("\nFROM");

            Command.AppendFormat("\n\t{0} {1} {2}", this.FullyQualifiedTableName, _d.WrapName(this.TableAlias, isGeneratedAlias: true), this.UseNoLock ? _d.NOLOCK : String.Empty);
            AddJoins(_t, Members.Values.ToList(), Command);

            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                SqlExpressionVisitor<T> visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Dialect = _d;
                visitor.Visit(_whereClause);
            }
            else if (_whereParameters != null)
            {
                Command.Append("\nWHERE\n\t1=1");
                if (_whereParameters is ValueType)
                {
                    var primaryKey = Members.Values.FirstOrDefault(m => m is ColumnMember && ((ColumnMember)m).IsPrimaryKey) as ColumnMember;
                    if(primaryKey != null)
                    {
                        var parameterName = System.Guid.NewGuid().ToString("N");
                        Command.AppendLine(String.Format("\n\tAND {0}.{1} = @{2}", _d.WrapName(primaryKey.TableAlias, isGeneratedAlias: true), primaryKey.FullyQualifiedName, parameterName));
                        Parameters.Add(parameterName, ((Object)_whereParameters).DBNullCoalese());
                    }
                    else
                    {
                        throw new InvalidOperationException("Trying to select an object by id without a primary key");
                    }
                }
                else
                {
                    foreach (KeyValuePair<String, Object> kvp in Execute.Utility.DecomposeObject(_whereParameters))
                    {
                        if (Members.ContainsKey(kvp.Key))
                        {
                            var member = Members[kvp.Key];
                            var paramName = System.Guid.NewGuid();
                            Parameters.Add(paramName.ToString("N"), kvp.Value);
                            Command.AppendFormat("\n\tAND {0}.{1} = @{2}", _d.WrapName(member.TableAlias, isGeneratedAlias: true), member.Name, paramName.ToString("N"));
                        }
                        else
                        {
                            var paramName = System.Guid.NewGuid();
                            Parameters.Add(paramName.ToString("N"), kvp.Value);
                            Command.AppendFormat("\n\tAND {0} = @{1}", _d.WrapName(kvp.Key, isGeneratedAlias: false), paramName.ToString("N"));
                        }
                    }
                }
            }

            if (_orderByClause != null && _orderByClause.Any())
            {
                Command.Append("\nORDER BY");
                Command.AppendFormat("\n\t{0}", GetOrderByClause());
            }
            
            if(Limit != null)
            {
                Command.AppendFormat("\nLIMIT {0}", Limit.Value);
            }
            if(Offset != null)
            {
                Command.AppendFormat("\nOFFSET {0}", Offset.Value);
            }
            Command.Append(";");

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\n\nSELECT * FROM {0};", _d.TempTablePrefix(this.OutputTableName));
                ProcessChildCollections();
                Command.AppendFormat("\nDROP TABLE {0};", _d.TempTablePrefix(this.OutputTableName));
            }

            return Command.ToString();
        }
    }
}