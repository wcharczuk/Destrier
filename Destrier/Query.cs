﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;

//var users = new Query<User>().Where(u => u.UserId > 10).Limit(1).Execute();
//var users = new Query<User>().Sql("select * from user_tbl where userid = @userId", new { userId = 2 }).Execute();

namespace Destrier
{
    /// <summary>
    /// Represents the abstract query body.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Query<T> where T : BaseModel
    {
        public Query() 
        {
            _command = new StringBuilder();
            _parameters = new Dictionary<String, Object>();
            _builder = new CommandBuilder<T>(_command, _parameters);
        }

        private IDictionary<String, Object> _parameters = null;
        private List<Member> _members = new List<Member>();
        private StringBuilder _command = null;
        private CommandBuilder<T> _builder = null;

        public Query<T> Limit(int resultSize)
        {
            _builder.Limit = resultSize;
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
            _builder.RemoveALlChildCollections();
            return this;
        }

        /// <summary>
        /// Evaluate the query and return an enumerable for streaming results.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Evaluate()
        {
            var type = typeof(T);
            if (ReflectionCache.HasReferencedObjectMembers(type) || ReflectionCache.HasChildCollectionMembers(type))
            {
                var list = new List<T>();
                Destrier.Execute.StatementReader(this.QueryBody, (dr) =>
                {
                    var objectLookups = new Dictionary<Type, Dictionary<Object, Object>>();
                    var parentDict = new Dictionary<Object, Object>();
                    objectLookups.Add(type, parentDict);

                    while (dr.Read())
                    {
                        T newObject = ReflectionCache.GetNewObject(type) as T;
                        Model.PopulateFullResults(newObject, dr, objectLookups: objectLookups, thisType: type);
                        list.Add(newObject);
                    }

                    if (_builder.ChildCollections.Any())
                    {
                        dr.NextResult();

                        foreach (var cm in _builder.ChildCollections)
                        {
                            var root = cm.Root;
                            var parent = cm.Parent ?? cm.Root;
                            var parentPrimaryKeyReference = cm.ReferencedProperty;

                            if (!objectLookups.ContainsKey(cm.CollectionType))
                            {
                                objectLookups.Add(cm.CollectionType, new Dictionary<Object, Object>());
                            }

                            dr.ReadIntoParentCollection(cm.CollectionType, (reader, obj) =>
                            {
                                var pkValue = parentPrimaryKeyReference.GetValue(obj);
                                var pkValueAsString = pkValue != null ? pkValue.ToString() : null;

                                var objPrimaryKeys = Model.ColumnsPrimaryKey(cm.CollectionType);

                                object objPrimaryKeyValue = Model.InstancePrimaryKeyValue(cm.CollectionType, obj);

                                if (objectLookups.ContainsKey(cm.CollectionType))
                                {
                                    if (!objectLookups[cm.CollectionType].ContainsKey(objPrimaryKeyValue))
                                    {
                                        objectLookups[cm.CollectionType].Add(objPrimaryKeyValue, obj);
                                    }
                                    else
                                    {
                                        obj = objectLookups[cm.CollectionType][objPrimaryKeyValue] as IPopulate;
                                    }
                                }

                                if (objectLookups[cm.DeclaringType].ContainsKey(pkValueAsString)) //if we have an instance of the parent
                                {
                                    var parentObj = objectLookups[cm.DeclaringType][pkValueAsString];
                                    var parentCollectionProperty = cm.Property;
                                    if (parentCollectionProperty.GetValue(parentObj) == null)
                                    {
                                        parentCollectionProperty.SetValue(parentObj, ReflectionCache.GetNewObject(cm.Type));
                                    }

                                    var collection = parentCollectionProperty.GetValue(parentObj);
                                    ((System.Collections.IList)collection).Add(obj);

                                }
                            }, populateFullResults: true);
                        }
                    }
                }, procedureParams: _parameters, connectionString: Model.ConnectionString(type), standardizeCasing: false);

                foreach (var item in list)
                    yield return item;
            }
            else
            {
                using(var cmd = Destrier.Execute.Command(Model.ConnectionString(type)))
                {
                    cmd.CommandText = this.QueryBody;
                    cmd.CommandType = System.Data.CommandType.Text;
                    Destrier.Execute.Utility.AddParametersToCommand(_parameters, cmd);
                    using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection), standardizeCasing: false))
                    {
                        while (dr.Read())
                        {
                            T newObject = ReflectionCache.GetNewObject(type) as T;
                            Model.PopulateFullResults(newObject, dr, thisType: type);

                            yield return newObject;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Execute the query.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Execute()
        {
            return this.Evaluate().ToList();
        }

        private String _queryBody = null;
        private String QueryBody
        {
            get
            {
                if (String.IsNullOrEmpty(_queryBody))
                    _queryBody = _builder.GenerateSelect();

                return _queryBody;
            }
            set
            {
                _queryBody = value;
            }
        }
    }
}
