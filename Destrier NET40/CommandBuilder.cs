using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Destrier.Extensions;

namespace Destrier
{
    public class CommandBuilder<T>
    {
        public CommandBuilder()
        {
            _t = typeof(T);
            _orderByClause = new List<OrderByElement>();

            RootMember = Model.GenerateRootMemberForType(_t);

            OutputTableName = RootMember.OutputTableName;
            TableAlias      = RootMember.TableAlias;
            UseNoLock       = RootMember.UseNoLock;

            Parameters = new Dictionary<String, Object>();
            FullyQualifiedTableName = Model.TableNameFullyQualified(_t);
            Command = new StringBuilder();

            _generateMemberMap();
            _setupTableAliases(this.Members.Values, _tableAliases);
        }

        protected Type _t = null;
        protected Dictionary<String, String> _tableAliases = new Dictionary<String, String>();

        public RootMember RootMember                { get; set; }
        public Dictionary<String, Member> Members   { get; set; }

        public String FullyQualifiedTableName   { get; private set; }
        public String OutputTableName           { get; private set; }
        public String TableAlias                { get; private set; }
        public Boolean UseNoLock                { get; private set; }

        public StringBuilder                    Command { get; set; }
        public IDictionary<String, Object>      Parameters { get; set; }

        protected Expression<Func<T, bool>>     _whereClause = null;
        protected dynamic                       _whereParameters = null;

        #region Select
        public Int32? Limit { get; set; }
        public Int32? Offset { get; set; }

        private List<OrderByElement>                  _orderByClause = null;
        private List<ChildCollectionMember>           _includedChildCollections = new List<ChildCollectionMember>();
        private List<Member>                          _outputMembers = new List<Member>();

        public Boolean                              HasChildCollections { get { return _includedChildCollections.Any(); } }
        public IEnumerable<ChildCollectionMember>   ChildCollections    { get { return _includedChildCollections;       } }
        
        class OrderByElement
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
            if (expression == null) { throw new ArgumentNullException("expression"); }
            _whereClause = expression;
        }

        public void AddWhereDynamic(dynamic parameters)
        {
            if (parameters == null) { throw new ArgumentNullException("parameters"); }
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

            MemberExpression memberExp = null;

            if (expression.Body is MemberExpression)
            {
                memberExp = expression.Body as MemberExpression;
            }
            else if (expression.Body is UnaryExpression)
            {
                memberExp = (expression.Body as UnaryExpression).Operand as MemberExpression;
            }

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

        #region Select Internals

        string _aliasedParentColumnName(ReferencedObjectMember m)
        {
            var parentAlias = String.Empty;
            if (m.Parent != null)
                parentAlias = m.Parent.TableAlias;
            else if (m.Root != null)
                parentAlias = m.Root.TableAlias;

            return String.Format("[{0}].[{1}]", parentAlias, m.ReferencedColumnMember.Name);
        }

        string _aliasedColumnName(ReferencedObjectMember m)
        {
            var pks = Model.ColumnsPrimaryKey(m.Type);
            var pk = pks.FirstOrDefault();
            if (pk == null)
            {
                throw new Exception("No Primary Key to join on.");
            }
            return String.Format("[{0}].[{1}]", m.TableAlias, pk.Name);
        }

        string _childCollectionParentColumnName(ChildCollectionMember ccm)
        {
            var lineage = new List<Member>();
            Member member = ccm.ParentReferencedMember;
            if (member != null)
            {
                lineage.Add(member);
                while (member.Parent != null && !(member.Parent is ChildCollectionMember))
                {
                    lineage.Add(member.Parent);
                    member = member.Parent;
                }
            }

            lineage.Reverse();
            return String.Join(".", lineage.Select(c => c.Name));
        }

        List<string> _getOutputColumns(IEnumerable<Member> members)
        {
            var outputColumns = new List<String>();

            foreach (var m in members)
            {
                outputColumns.Add(String.Format("[{0}].[{1}] as [{2}]"
                    , m.TableAlias
                    , m.Name
                    , m.FullyQualifiedName
                    ));
            }
            return outputColumns;
        }

        List<string> _getOutputPrimaryKeyMembers(IEnumerable<Member> members)
        {
            var outputColumns = new List<String>();

            foreach (var m in members.Select(m => m as ColumnMember).Where(cm => cm != null && cm.IsPrimaryKey && cm.Parent == null))
            {
                outputColumns.Add(String.Format("[{0}].[{1}] asc"
                    , m.TableAlias
                    , m.Name
                    ));
            }
            return outputColumns;
        }

        string _getOrderByClause(String tableAlias = null, Boolean useTableQualifier = true, Boolean useFullyQualifiedName = false)
        {
            if (useTableQualifier)
            {
                var values = _orderByClause.Select(o => String.Format("[{0}].[{1}] {2}"
                    , tableAlias ?? ((ColumnMember)o.Member).TableAlias
                    , useFullyQualifiedName ? ((ColumnMember)o.Member).FullyQualifiedName : ((ColumnMember)o.Member).Name
                    , o.Ascending ? "ASC" : "DESC")
                    );
                return string.Join(",", values);
            }
            else
            {
                var values = _orderByClause.Select(o => String.Format("[{0}] {1}"
                    , useFullyQualifiedName ? ((ColumnMember)o.Member).FullyQualifiedName : ((ColumnMember)o.Member).Name
                    , o.Ascending ? "ASC" : "DESC")
                    );
                return string.Join(",", values);
            }
        }

        void _generateMemberMap()
        {
            this.Members = new Dictionary<String, Member>();
            var memberList = Model.GenerateAllMembers(_t, RootMember, null);

            foreach (var m in memberList)
            {
                Members.Add(m.FullyQualifiedName, m);

                if (m is ColumnMember && !m.AnyParent(p => p is ChildCollectionMember))
                {
                    _outputMembers.Add(m);
                }

                var col_m = m as ChildCollectionMember;
                if (
                    col_m != null
                    && col_m.AlwaysInclude
                    && !col_m.AnyParent(p => p is ChildCollectionMember && !((ChildCollectionMember)p).AlwaysInclude)
                    && !col_m.IsLazy
                )
                {
                    _includedChildCollections.Add(col_m);
                }
            }
        }

        void _setupTableAliases(IEnumerable<Member> members, Dictionary<String, String> tableAliases)
        {
            foreach (ReferencedObjectMember refm in members.Where(m => m is ReferencedObjectMember && !m.IsLazy))
            {
                if (!tableAliases.ContainsKey(refm.FullyQualifiedName))
                {
                    tableAliases.Add(refm.FullyQualifiedName, Model.GenerateAlias());
                }
                refm.TableAlias = tableAliases[refm.FullyQualifiedName];
                refm.OutputTableName = Model.GenerateAlias();
            }

            foreach (ChildCollectionMember cm in members.Where(m => m is ChildCollectionMember && !m.IsLazy))
            {
                if (!tableAliases.ContainsKey(cm.FullyQualifiedName))
                {
                    tableAliases.Add(cm.FullyQualifiedName, Model.GenerateAlias());
                }
                cm.TableAlias = tableAliases[cm.FullyQualifiedName];
                cm.OutputTableName = Model.GenerateAlias();
            }

            foreach (ColumnMember cm in members.Where(m => m is ColumnMember))
            {
                if (cm.Parent != null && !cm.Parent.IsLazy && tableAliases.ContainsKey(cm.Parent.FullyQualifiedName)) 
                {
                    cm.TableAlias = tableAliases[cm.Parent.FullyQualifiedName];
                }
                else
                {
                    cm.TableAlias = cm.Root.TableAlias;
                }
            }
        }

        #endregion

        public virtual String GenerateSelect()
        {
            Command = new StringBuilder();
            var columnList = String.Join("\n\t, ", _getOutputColumns(_outputMembers));

            if (Offset != null)
            {
                if (Limit != null)      { Command.AppendFormat("SELECT TOP {0} * ", Limit.Value); }
                else                    { Command.AppendFormat("SELECT * "); }

                if (_includedChildCollections.Any()) { Command.AppendFormat("\nINTO #{0}", this.OutputTableName); }

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
                    Command.AppendFormat(" OVER (order by {0})", _getOrderByClause());
                }
                else
                {
                    var primaryKeys = _getOutputPrimaryKeyMembers(_outputMembers);
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
            _addJoins(_t, Members.Values.ToList(), Command);

            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");
                new SqlExpressionVisitor<T>(Command, Parameters, Members).Visit(_whereClause);
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
                Command.AppendFormat("\n\t{0}", _getOrderByClause());
            }

            if (Offset != null)
            {
                Command.Append("\n) as [data]");
                Command.AppendFormat("\nWHERE [row_number_for_offset] > {0}", Offset.Value.ToString());
            }

            if (Offset != null && _orderByClause.Any())
            {
                Command.Append("\nORDER BY");
                Command.AppendFormat("\n\t{0}", _getOrderByClause(tableAlias: "data", useFullyQualifiedName:true));
            }

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\n\nSELECT * FROM #{0}", this.OutputTableName);

                if (_orderByClause.Any())
                {
                    Command.Append("\nORDER BY");
                    Command.AppendFormat("\n\t{0}", _getOrderByClause(useTableQualifier:false, useFullyQualifiedName:true));
                }

                _processChildCollections();
                Command.AppendFormat("\nDROP TABLE #{0}", this.OutputTableName);
            }

            return Command.ToString();
        }

        void _addJoins(Type t, List<Member> members, StringBuilder command)
        {
            if (ModelCache.HasReferencedObjectMembers(t))
            {
                foreach (var rom in members.Where(m => m is ReferencedObjectMember && !m.IsLazy && !m.AnyParent(rm => rm is ChildCollectionMember)).Select(m => m as ReferencedObjectMember))
                {
                    command.AppendFormat("\n\t{0} JOIN {1} [{2}] {5} on {3} = {4}",
                        rom.JoinType, //0
                        rom.FullyQualifiedTableName, //1
                        rom.TableAlias, //2
                        _aliasedParentColumnName(rom), //3
                        _aliasedColumnName(rom), //4
                        rom.UseNoLock ? "(NOLOCK)" : String.Empty //5
                    );
                }
            }
        }

        void _processChildCollections()
        {
            List<String> tablesToDrop = new List<String>();
            foreach (var cm in ChildCollections)
            {
                var subMembers = Model.GenerateAllMembers(cm.CollectionType, new RootMember(cm), null);
                var hasChildMembers = subMembers.Any(m => m is ChildCollectionMember && !m.IsLazy && !m.AnyParent(p => p.IsLazy));

                var tableAliases = new Dictionary<String, String>();
                _setupTableAliases(subMembers, tableAliases);

                var outputColumns = _getOutputColumns(subMembers.Where(m =>
                    m is ColumnMember
                    && !m.AnyParent(p => p is ChildCollectionMember || p.IsLazy))
                );

                var subColumnList = String.Join("\n\t, ", outputColumns);
                Command.AppendFormat("\n\nSELECT\n\t{0}", subColumnList);

                if (hasChildMembers)
                {
                    Command.AppendFormat("\nINTO #{0}", cm.OutputTableName);
                    tablesToDrop.Add(cm.OutputTableName);
                }

                // the parent needs to be the next rung up, or the root, or the next run up that isn't a ref obj
                var parent = cm.Parent ?? cm.Root;
                if (parent is ReferencedObjectMember)
                {
                    while (parent is ReferencedObjectMember)
                    {
                        parent = parent.Parent;
                    }
                    parent = parent ?? cm.Root;   
                }

                Command.AppendFormat("\nFROM");
                Command.AppendFormat("\n\t#{0} [{1}]", parent.OutputTableName, parent.TableAlias);

                Command.AppendFormat("\n\t{0} JOIN {1} [{2}] {5} ON {3} = {4}"
                    , cm.JoinType //0
                    , cm.FullyQualifiedTableName //1
                    , cm.TableAlias //2
                    , String.Format("[{0}].[{1}]", parent.TableAlias, _childCollectionParentColumnName(cm)) //3
                    , String.Format("[{0}].[{1}]", cm.TableAlias, cm.ReferencedColumnName) //4
                    , cm.UseNoLock ? "(NOLOCK)" : String.Empty);

                //add the rest of the referenced joins
                _addJoins(cm.CollectionType, subMembers, Command);

                Command.Append(";");

                if (hasChildMembers)
                {
                    Command.AppendFormat("\n\nSELECT * FROM #{0};", cm.OutputTableName);
                }
            }

            Command.Append("\n");

            foreach (var table in tablesToDrop)
            {
                Command.AppendFormat("\nDROP TABLE #{0};", table);
            }
        }

        public virtual String GenerateUpdate()
        {
            Command = new StringBuilder();
            Command.Append("UPDATE\n");
            Command.AppendFormat("\t[{0}]", this.TableAlias);
            Command.Append("\nSET\n");

            Command.Append(String.Format("\t{0}", String.Join(",", _updateSets)));
            Command.Append("\nFROM");
            Command.AppendFormat("\n\t{0} [{1}]", this.FullyQualifiedTableName, this.TableAlias);
            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                var visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Visit(_whereClause);
            }

            return Command.ToString();
        }
    }
}