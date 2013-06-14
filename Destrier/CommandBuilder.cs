using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public class CommandBuilder<T>
    {
        public CommandBuilder()
        {
            this._t = typeof(T);
            Initialize();
        }

        public CommandBuilder(StringBuilder command)
            : this()
        {
            this.Command = command;
        }

        public CommandBuilder(StringBuilder command, IDictionary<String, Object> parameters)
            : this()
        {
            this.Command = command;
            this.Parameters = parameters;
        }

        private void Initialize()
        {
            this._t = typeof(T);
            this.Parameters = new Dictionary<String, Object>();
            this.FullyQualifiedTableName = Model.TableNameFullyQualified(_t);

            this.Command = new StringBuilder();
            this.Parameters = new Dictionary<String, Object>();
            
            DiscoverMembers();
            SetupTableAliases(this.Members.Values, _tableAliases);
        }

        protected Type _t { get; set; }
        protected Dictionary<String, String> _tableAliases = new Dictionary<String, String>();
        
        protected List<ChildCollectionMember> _includedChildCollections = new List<ChildCollectionMember>();
        protected List<Member> _outputMembers = new List<Member>();

        public IEnumerable<ChildCollectionMember> ChildCollections { get { return _includedChildCollections.OrderByDescending(cm => Model.HasChildCollections(cm.CollectionType)); } }

        public Int32? Limit { get; set; }
        public StringBuilder Command { get; set; }
        public IDictionary<String, Object> Parameters { get; set; }

        public Dictionary<String, Member> Members { get; set; }
        
        public String FullyQualifiedTableName { get; private set; }
        public String TableAlias { get { return this.AsRootMember.TableAlias; } }
        public String OutputTableName { get { return this.AsRootMember.OutputTableName; } }
        public RootMember AsRootMember { get { return ReflectionCache.GetRootMemberForType(_t); } }

        private Expression<Func<T, bool>> _whereClause = null;
        private dynamic _whereParameters = null;
        private String _orderByClause = null;

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

        public void AddOrderBy<F>(Expression<Func<T, F>> expression)
        {
            var body = (MemberExpression)expression.Body;

            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause = String.Format("[{0}].[{1}] ASC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddOrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            var body = (MemberExpression)expression.Body;

            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause = String.Format("[{0}].[{1}] DESC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddThenOrderBy<F>(Expression<Func<T, F>> expression)
        {
            if (String.IsNullOrEmpty(_orderByClause))
                throw new Exception("Need to run OrderBy or OrderByDescending first!");

            var body = (MemberExpression)expression.Body;
            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause = _orderByClause + String.Format(", [{0}].[{1}] ASC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddThenOrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            if (String.IsNullOrEmpty(_orderByClause))
                throw new Exception("Need to run OrderBy or OrderByDescending first!");

            var body = (MemberExpression)expression.Body;
            var columnMember = Model.MemberForExpression(body, Members);

            if (columnMember == null)
                throw new Exception("Invalid Member. Members must be either marked Column or be a child of a ReferencedObject.");

            _orderByClause = _orderByClause + String.Format(", [{0}].[{1}] DESC", columnMember.TableAlias, columnMember.Name);
        }

        public void AddIncludedChildCollection<F>(Expression<Func<T, F>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var body = (MemberExpression)expression.Body;
            var member = Model.MemberForExpression(body, Members) as ChildCollectionMember;

            if (member != null)
                if (!_includedChildCollections.Any(ic => ic.Equals(member)))
                    _includedChildCollections.Add(member);
        }

        public void AddIncludedChildCollection(String fullyQualifiedMemberName)
        {
            if (Members.ContainsKey(fullyQualifiedMemberName))
            {
                var member = Members[fullyQualifiedMemberName] as ChildCollectionMember;
                if (member != null)
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
            foreach (var member in Members.Values.Where(m => m is ChildCollectionMember))
            {
                if (!_includedChildCollections.Any(ic => ic.Equals(member)))
                    _includedChildCollections.Add(member as ChildCollectionMember);
            }
        }

        public void RemoveALlChildCollections()
        {
            _includedChildCollections.Clear();
        }

        private static String GenerateTableAlias()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        private static List<String> GetOutputColumns(IEnumerable<Member> members)
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

        private void DiscoverMembers()
        {
            this.Members = new Dictionary<String, Member>();
            var memberList = ReflectionCache.MembersRecursiveCached(_t);

            foreach (var m in memberList)
            {
                Members.Add(m.FullyQualifiedName, m);

                if (m is ColumnMember && !m.ParentAny(p => p is ChildCollectionMember))
                    _outputMembers.Add(m);

                var col_m = m as ChildCollectionMember;
                if (col_m != null
                && col_m.AlwaysInclude
                && !col_m.ParentAny(p => p is ChildCollectionMember && !((ChildCollectionMember)p).AlwaysInclude)
                )
                {
                    _includedChildCollections.Add(col_m);
                }
            }
        }

        private static void SetupTableAliases(IEnumerable<Member> members, Dictionary<String, String> tableAliases)
        {
            foreach (ReferencedObjectMember refm in members.Where(m => m is ReferencedObjectMember))
            {
                if (!tableAliases.ContainsKey(refm.FullyQualifiedName))
                {
                    tableAliases.Add(refm.FullyQualifiedName, GenerateTableAlias());
                }
                refm.TableAlias = tableAliases[refm.FullyQualifiedName];
            }
            foreach (ChildCollectionMember cm in members.Where(m => m is ChildCollectionMember))
            {
                if (!tableAliases.ContainsKey(cm.FullyQualifiedName))
                {
                    tableAliases.Add(cm.FullyQualifiedName, GenerateTableAlias());
                }
                cm.TableAlias = tableAliases[cm.FullyQualifiedName];

                if (Model.HasChildCollections(cm.CollectionType))
                {
                    cm.OutputTableName = GenerateTableAlias();
                }
            }
            foreach (ColumnMember cm in members.Where(m => m is ColumnMember))
            {
                if (cm.Parent != null)
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

        private static void AddJoins(Type t, List<Member> members, StringBuilder command)
        {
            if (Model.HasReferencedObjects(t))
            {
                foreach (var rom in members.Where(m => m is ReferencedObjectMember && !m.ParentAny(rm => rm is ChildCollectionMember)).Select(m => m as ReferencedObjectMember))
                {
                    command.AppendFormat("\n\t{0} JOIN {1} [{2}] (NOLOCK) on {3} = {4}",
                        rom.JoinType,
                        rom.FullyQualifiedTableName,
                        rom.TableAlias,
                        rom.AliasedParentColumnName,
                        rom.AliasedColumnName
                    );
                }
            }
        }

        public String GenerateSelect()
        {
            var columnList = String.Join("\n\t, ", GetOutputColumns(_outputMembers));

            if (Limit != null)
            {
                Command.AppendFormat("SELECT top(@RESULT_LIMIT)\n\t{0}", columnList);
                Parameters.Add("RESULT_LIMIT", Limit.Value);
            }
            else
            {
                Command.AppendFormat("SELECT\n\t{0}", columnList);
            }

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\nINTO #{0}", this.OutputTableName);
                Command.Append("\nFROM");
            }
            else
            {
                Command.Append("\nFROM");
            }

            Command.AppendFormat("\n\t{0} [{1}] (NOLOCK)", this.FullyQualifiedTableName, this.TableAlias);
            AddJoins(_t, Members.Values.ToList(), Command);

            if (_whereClause != null)
            {
                Command.Append("\nWHERE\n");

                SqlExpressionVisitor<T> visitor = new SqlExpressionVisitor<T>(Command, Parameters, Members);
                visitor.Visit(_whereClause);
            }
            else if (_whereParameters != null)
            {
                Command.Append("\nWHERE\n\t1=1");
                if (_whereParameters is ValueType)
                {
                    if (Model.ColumnsPrimaryKey(typeof(T)).FirstOrDefault() == null)
                        throw new InvalidOperationException("Trying to select an object by id without a primary key");

                    Command.AppendLine(String.Format("\n\tAND [{0}] = @{0}", Model.ColumnsPrimaryKey(typeof(T)).FirstOrDefault().Name));
                    Parameters.Add(String.Format("{0}", Model.ColumnsPrimaryKey(typeof(T)).FirstOrDefault().Name), ((Object)_whereParameters).DBNullCoalese());
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

            if (!String.IsNullOrEmpty(_orderByClause))
            {
                Command.Append("\nORDER BY");
                Command.AppendFormat("\n\t{0}", _orderByClause);
            }

            if (_includedChildCollections.Any())
            {
                Command.AppendFormat("\n\nSELECT * FROM #{0};", this.OutputTableName);
                ProcessChildCollections();
                Command.AppendFormat("\nDROP TABLE #{0}", this.OutputTableName);
            }

            return Command.ToString();
        }

        public void ProcessChildCollections()
        {
            List<String> tablesToDrop = new List<String>();
            foreach (var cm in ChildCollections)
            {
                var subMembers = ReflectionCache.MembersRecursive(cm.CollectionType, new RootMember(cm));

                var tableAliases = new Dictionary<String, String>();
                SetupTableAliases(subMembers, tableAliases);

                var outputColumns = GetOutputColumns(subMembers.Where(m => m is ColumnMember && !m.ParentAny(p => p is ChildCollectionMember)));

                var subColumnList = String.Join("\n\t, ", outputColumns);
                Command.AppendFormat("\n\nSELECT\n\t{0}", subColumnList);

                if (Model.HasChildCollections(cm.CollectionType))
                {
                    Command.AppendFormat("\nINTO #{0}", cm.OutputTableName);
                    tablesToDrop.Add(cm.OutputTableName);
                }

                Command.AppendFormat("\nFROM");

                var root = this.AsRootMember;
                var parent = cm.Parent ?? root;
                Command.AppendFormat("\n\t#{0} [{1}]", parent.OutputTableName ?? root.OutputTableName, parent.TableAlias);

                Command.AppendFormat("\n\t{0} JOIN {1} [{2}] (NOLOCK) ON {3} = {4}", cm.JoinType, cm.FullyQualifiedTableName, cm.TableAlias, cm.AliasedParentColumnName, cm.AliasedColumnName);
                AddJoins(cm.CollectionType, subMembers, Command);

                if (Model.HasChildCollections(cm.CollectionType))
                {
                    Command.AppendFormat("\n\nSELECT * FROM #{0};", cm.OutputTableName);
                }
            }

            Command.Append("\n");

            foreach (var table in tablesToDrop)
            {
                Command.AppendFormat("\nDROP TABLE #{0}", table);
            }
        }
    }
}