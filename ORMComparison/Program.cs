using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Destrier;

namespace ORMComparison
{
    public enum TestObjectTypeId 
    {
        One = 1,
        Two = 2,
        Three = 3
    }

    [Destrier.Table("TestObjects")]
    [ServiceStack.DataAnnotations.Alias("TestObjects")]
    public class TestObject
    {
        [Destrier.Column(IsPrimaryKey = true, CanBeNull = false)]
        [ServiceStack.DataAnnotations.PrimaryKey]
        public Int32 Id { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Boolean Active { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public String Name { get; set; }

        [Destrier.Column]
        public String NullName { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public DateTime Created { get; set; }

        [Destrier.Column]
        public DateTime? Modified { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Int32 ReferencedObjectId { get; set; }

        [Destrier.Column]
        public Int32? NullableId { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public TestObjectTypeId Type { get; set; }

        [Destrier.Column]
        public Int32? NullableType { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public String SingleChar { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Double Single { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Double Double { get; set; }

        [Destrier.Column]
        public Double? NullableDouble { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Guid Guid { get; set; }

        [Destrier.Column]
        public Guid? NullableGuid { get; set; }
    }

    public class Program
	{
		public const int TRIALS = 100;
		public const int LIMIT = 5000;

        static void Main(string[] args)
        {
            var testObjectContext = new Destrier.Test.TestObjectContext();

            string QUERY = new Query<TestObject>().Limit(LIMIT).QueryBody;
            string CONNECTION_STRING = Destrier.DatabaseConfigurationContext.DefaultConnectionString;

            Func<List<TestObject>> rawAction = () =>
            {
				var provider = Destrier.DatabaseConfigurationContext.DefaultProviderFactory;

                var list = new List<TestObject>();
				using (var conn = provider.CreateConnection())
                {
					conn.ConnectionString = DatabaseConfigurationContext.DefaultConnectionString;
                    conn.Open();
                    using (var cmd = provider.CreateCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = QUERY;
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var testObject = ReflectionHelper.GetNewObject<TestObject>();
                               
								testObject.Id = dr.GetInt32(0);
                                testObject.Active = dr.GetBoolean(1);
                                testObject.Name = dr.GetString(2);
								testObject.NullName = !dr.IsDBNull(3) ? dr.GetString(3) : null;
								testObject.Created = dr.GetDateTime(4);
								testObject.Modified = !dr.IsDBNull(5) ? (DateTime?)dr.GetDateTime(5) : null;
								testObject.NullableId = !dr.IsDBNull(6) ? (int?)dr.GetInt32(6) : null;
								testObject.ReferencedObjectId = !dr.IsDBNull(7) ? dr.GetInt32(7) : default(Int32);
								testObject.Type = (TestObjectTypeId)dr.GetInt32(8);
								testObject.NullableType = !dr.IsDBNull(9) ? (Int32?)dr.GetInt32(9) : null;
								testObject.SingleChar = dr.GetString(10);
								testObject.Single = (Single)dr.GetDouble(11);
								testObject.Double = !dr.IsDBNull(12) ? dr.GetDouble(12) : default(Double);
								testObject.NullableDouble = !dr.IsDBNull(13) ? (Double?)dr.GetDouble(13) : null;
								testObject.Guid = !dr.IsDBNull(14) ? dr.GetGuid(14) : default(Guid);
								testObject.NullableGuid = !dr.IsDBNull(15) ? (Guid?)dr.GetGuid(15) : null;

                                list.Add(testObject);
                            }
                        }
                    }
                }
                return list;
            };

            Func<List<TestObject>> ormLiteAction = () =>
            {
                var dbFactory = new ServiceStack.OrmLite.OrmLiteConnectionFactory(CONNECTION_STRING, ServiceStack.OrmLite.SqlServerDialect.Provider);
                using (System.Data.IDbConnection db = dbFactory.OpenDbConnection())
                {
                    return ServiceStack.OrmLite.ReadConnectionExtensions.Select<TestObject>(db, q => q.Limit(LIMIT));
                }
            };


            Func<List<TestObject>> destrierAction = () =>
            {
                return new Destrier.Query<TestObject>().Limit(LIMIT).StreamResults().ToList();
            };

            Func<List<TestObject>> destrierRawQueryAction = () =>
            {
                return new Destrier.Query<TestObject>(QUERY).StreamResults().ToList();
            };

			
            Func<List<TestObject>> petaPocoAction = () =>
                {
                    var db = new PetaPoco.Database(DatabaseConfigurationContext.DefaultConnectionString, "System.Data.SqlClient");
                    return db.Query<TestObject>(QUERY).ToList();
                };
            

            Func<List<TestObject>> dapperAction = () =>
            {
                using (var conn = Destrier.DatabaseConfigurationContext.DefaultProviderFactory.CreateConnection())
                {
                    conn.ConnectionString = CONNECTION_STRING;
                    conn.Open();
                    return conn.Query<TestObject>(QUERY).ToList();
                }
            };

            //-------------------------------------------------------------------------------------------
            //-------------------------------------------------------------------------------------------
            //Run Tests
            //-------------------------------------------------------------------------------------------
            //-------------------------------------------------------------------------------------------

            var stopwatch = new System.Diagnostics.Stopwatch();

            Console.WriteLine("ORM Test Suite");
            for (int x = 0; x < Console.WindowWidth; x++)
            {
                Console.Write("=");
            }
            Console.WriteLine();

            var testSteps = new Dictionary<string, Func<List<TestObject>>>()
            {
                { "Raw Reader", rawAction },
                { "Destrier", destrierAction },
                { "Destrier (Raw Query)", destrierRawQueryAction },
                { "PetaPoco", petaPocoAction },
                { "ServiceStack ORMLite", ormLiteAction },
                { "Dapper", dapperAction },
            };

            var results = new List<Int64>();

			foreach (var kvp in testSteps)
			{
				stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				var queryResults = kvp.Value();
				stopwatch.Stop();

				if (!queryResults.Any())
					throw new Exception("No results.");
			}

            foreach (var kvp in testSteps)
            {
                results = new List<long>();
                for (int x = 0; x < TRIALS; x++)
                {
                    stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    var queryResults = kvp.Value();
                    stopwatch.Stop();
                    results.Add(stopwatch.ElapsedMilliseconds);

                    if (!queryResults.Any())
                        throw new Exception("No results.");
                }
                
                Console.Write(kvp.Key);
                int spaces = 25 - kvp.Key.Length;
                for (int x = 0; x < spaces; x++)
                    Console.Write(" ");

                Console.Write(String.Format("\tFirst Result: {0}ms", results.First()));
                Console.WriteLine(String.Format("\tAvg: {0}ms", results.Average()));
            }
            Console.WriteLine();

            testObjectContext.EnsureDestroyDataStore();
        }
    }
}
