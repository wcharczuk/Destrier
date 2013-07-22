using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Collections;

namespace Destrier
{
    /// <summary>
    /// Represents the abstract query body.
    /// </summary>
    /// <typeparam name="T">Type to populate and return.</typeparam>
    public class Query<T> where T : new()
    {
        public Query()
        {
			StandardizeCasing = false;
            _command = new StringBuilder();
            _parameters = new Dictionary<String, Object>();
            _builder = CommandBuilderFactory.GetCommandBuilder<T>();
            _builder.Command = _command;
            _builder.Parameters = _parameters; 
            _t = typeof(T);
        }

        public Query(String query)
        {
			StandardizeCasing = true;
            _command = new StringBuilder();
            _parameters = new Dictionary<String, Object>();
            _t = typeof(T);
            _queryBody = query;
        }

        public Query(String query, IDictionary<String, Object> parameters)
            : this(query)
        {
            _parameters = parameters;
        }

        private IDictionary<String, Object> _parameters = null;
        private List<Member> _members = new List<Member>();
        private StringBuilder _command = null;
        private CommandBuilder<T> _builder = null;

		public Boolean StandardizeCasing { get; set; }

        private Type _t = null;

        public Query<T> Limit(int resultSize)
        {
            _builder.Limit = resultSize;
            return this;
        }

        public Query<T> Offset(int offsetSize)
        {
            _builder.Offset = offsetSize;
            return this;
        }

        public Query<T> Where(Expression<Func<T, Boolean>> expression)
        {
            _builder.AddWhere(expression);
            return this;
        }

        public Query<T> Where(dynamic parameters)
        {
            _builder.AddWhereDynamic(parameters);
            return this;
        }

        public Query<T> OrderBy<F>(Expression<Func<T, F>> expression)
        {
            _builder.AddOrderBy<F>(expression);
            return this;
        }

        public Query<T> OrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            _builder.AddOrderByDescending<F>(expression);
            return this;
        }

        public Query<T> ThenOrderBy<F>(Expression<Func<T, F>> expression)
        {
            _builder.AddThenOrderBy<F>(expression);
            return this;
        }

        public Query<T> ThenOrderByDescending<F>(Expression<Func<T, F>> expression)
        {
            _builder.AddThenOrderByDescending<F>(expression);
            return this;
        }

        public Query<T> Include<F>(Expression<Func<T, F>> expression)
        {
            _builder.AddIncludedChildCollection(expression);
            return this;
        }

        public Query<T> Include(String fullyQualifiedMemberName)
        {
            _builder.AddIncludedChildCollection(fullyQualifiedMemberName);
            return this;
        }

        public Query<T> DontInclude<F>(Expression<Func<T, F>> expression)
        {
            _builder.RemoveChildCollection(expression);
            return this;
        }

        public Query<T> DontInclude(String fullyQualifiedMemberName)
        {
            _builder.RemoveChildCollection(fullyQualifiedMemberName);
            return this;
        }

        public Query<T> IncludeAll()
        {
            _builder.AddIncludeAll();
            return this;
        }

        public Query<T> DontIncludeAny()
        {
            _builder.RemoveAllChildCollections();
            return this;
        }

        /// <summary>
        /// Evaluate the query and return an enumerable for streaming results.
        /// </summary>
        /// <returns>An enumerable</returns>
        public IEnumerable<T> StreamResults()
        {
            if (_builder != null && _builder.HasChildCollections)
                return _slowPipeline();
            else
                return _fastPipeline();
        }

        private IEnumerable<T> _slowPipeline()
        {
            var list = new List<T>();
            var connectionName = Model.ConnectionName(_t);
            using (var cmd = Destrier.Execute.Command(connectionName))
            {
                cmd.CommandText = this.QueryBody;
                cmd.CommandType = System.Data.CommandType.Text;
                Destrier.Execute.Utility.AddParametersToCommand(_parameters, cmd, connectionName);

                using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection), type: _t, standardizeCasing: false))
                {
                    var objectLookups = new Dictionary<Type, Dictionary<Object, Object>>();
                    var parentDict = new Dictionary<Object, Object>();
                    objectLookups.Add(_t, parentDict);

                    while (dr.Read())
                    {
                        T newObject = (T)ReflectionCache.GetNewObject(_t);
                        Model.PopulateFullResults(newObject, dr, objectLookups: objectLookups, thisType: _t);
                        list.Add(newObject);
                    }

                    if (_builder.ChildCollections.Any())
                    {
                        foreach (var cm in _builder.ChildCollections.Where(cm => !cm.IsLazy))
                        {
                            var root = cm.Root;
                            var parent = cm.Parent ?? cm.Root;
                            var referencedParentPrimaryKey = cm.ReferencedProperty;

                            dr.NextResult(cm.CollectionType);

                            if (!objectLookups.ContainsKey(cm.CollectionType))
                                objectLookups.Add(cm.CollectionType, new Dictionary<Object, Object>());

                            dr.ReadIntoParentCollection(cm.CollectionType, (reader, obj) =>
                            {
                                var pkValue = referencedParentPrimaryKey.GetValue(obj);
                                var pkValueAsString = pkValue != null ? pkValue.ToString() : null;

                                var objPrimaryKeys = Model.ColumnsPrimaryKey(cm.CollectionType);

                                object objPrimaryKeyValue = Model.InstancePrimaryKeyValue(cm.CollectionType, obj);
                                    
                                object parentObj = null;
                                objectLookups[cm.DeclaringType].TryGetValue(pkValueAsString, out parentObj);

                                if (parentObj != null)
                                {
                                    var parentCollectionProperty = cm.Property;

                                    //new up the collection if it hasn't been delcared yet.
                                    if (parentCollectionProperty.GetValue(parentObj) == null)
                                        parentCollectionProperty.SetValue(parentObj, ReflectionCache.GetNewObject(cm.Type));

                                    //set up lookup for the object
                                    Dictionary<Object, Object> objLookup = null;
                                    objectLookups.TryGetValue(cm.CollectionType, out objLookup);

                                    if (objLookup != null && objPrimaryKeyValue != null)
                                        if (!objLookup.ContainsKey(objPrimaryKeyValue))
                                            objLookup.Add(objPrimaryKeyValue, obj);
                                        else
                                            obj = objLookup[objPrimaryKeyValue] as IPopulate;

                                    var collection = parentCollectionProperty.GetValue(parentObj);
                                    ((System.Collections.IList)collection).Add(obj);
                                }
                            }, populateFullResults: true, advanceToNextResultAfter: false);
                            
                        }
                    }
                }
            }
            return list;
        }

        private IEnumerable<T> _fastPipeline()
        {
            var connectionName = Model.ConnectionName(_t);
            var dialect = _builder != null ? _builder.Dialect : SqlDialectVariantFactory.GetSqlDialect(connectionName);
            var shouldStandardizeCasing = dialect.AltersOutputCasing && this.StandardizeCasing;
            
            using (var cmd = Destrier.Execute.Command(connectionName))
            {
                cmd.CommandText = this.QueryBody;
                cmd.CommandType = System.Data.CommandType.Text;
                Destrier.Execute.Utility.AddParametersToCommand(_parameters, cmd, connectionName);
                using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection), type: _t, standardizeCasing: shouldStandardizeCasing))
                {
                    while (dr.Read())
                    {
                        T newObject = (T)ReflectionCache.GetNewObject(_t);
                        Model.PopulateFullResults(newObject, dr, _t);
                        yield return newObject;
                    }
                }
            }
        }

        /// <summary>
        /// Execute and enumerate the results of the query.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Execute()
        {
            return StreamResults().ToList();
        }

        private String _queryBody = null;
        public String QueryBody
        {
            get
            {
                if (String.IsNullOrEmpty(_queryBody))
                    _queryBody = _builder.GenerateSelect();

                return _queryBody;
            }
            set
            {
				StandardizeCasing = true;
                _queryBody = value;
            }
        }
    }
}
