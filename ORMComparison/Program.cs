using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ORMComparison
{
    public class MockObjectContext : DbContext
    {
        public MockObjectContext() : base("Data Source=.;Initial Catalog=tempdb;Integrated Security=True") { }

        public DbSet<TestObject> MockObjects { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new TestObjectMap());
        }
    }

    public class TestObjectMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<TestObject>
    {
        public TestObjectMap()
        {
            this.HasKey(t => t.Id);
            this.Property(t => t.Id);
            this.Property(t => t.Name);
            this.Property(t => t.Active);
            this.Property(t => t.Created);
            this.Property(t => t.Modified);
            this.Property(t => t.ReferencedObjectId);
            this.Property(t => t.NullableId);
            this.ToTable("TestObjects");
        }
    }

    [Destrier.Table("TestObjects")]
    [ServiceStack.DataAnnotations.Alias("TestObjects")]
    public class TestObject
    {
        [Destrier.Column(IsPrimaryKey = true)]
        [ServiceStack.DataAnnotations.PrimaryKey]
        public Int32 Id { get; set; }

        [Destrier.Column]
        public Boolean Active { get; set; }

        [Destrier.Column]
        public String Name { get; set; }

        [Destrier.Column]
        public DateTime Created { get; set; }

        [Destrier.Column]
        public DateTime? Modified { get; set; }

        [Destrier.Column]
        public Int32 ReferencedObjectId { get; set; }

        [Destrier.Column]
        public Int32? NullableId { get; set; }
    }

    public class Program
    {
		public const String ConnectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=True";
        public const int TRIALS = 100;
        public const int LIMIT = 2000;

        static void Main(string[] args)
        {
            Destrier.DatabaseConfigurationContext.ConnectionStrings.Add("default", ConnectionString);
            Destrier.DatabaseConfigurationContext.DefaultConnectionName = "default";
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";

            Destrier.Test.DatabaseTest.EnsureInitDataStore();

            string QUERY = String.Format("SELECT TOP {0} Id, Name, Active, Created, Modified, NullableId, ReferencedObjectId from TestObjects (nolock)", LIMIT);

            Func<List<TestObject>> rawAction = () =>
            {
                var list = new List<TestObject>();
                using (var conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;

                        cmd.CommandText = QUERY;
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (var dr = cmd.ExecuteReader(System.Data.CommandBehavior.Default))
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    var mockObject = Destrier.ReflectionCache.GetNewObject<TestObject>();
                                    mockObject.Id = dr.GetInt32(0);
                                    mockObject.Name = dr.GetString(1);
                                    mockObject.Active = dr.GetBoolean(2);
                                    mockObject.Created = dr.GetDateTime(3);
                                    mockObject.Modified = !dr.IsDBNull(4) ? (DateTime?)dr.GetDateTime(4) : null;
                                    mockObject.NullableId = !dr.IsDBNull(5) ? (int?)dr.GetInt32(5) : null;
                                    mockObject.ReferencedObjectId = dr.GetInt32(6);

                                    list.Add(mockObject);
                                }
                            }
                        }
                    }
                }
                return list;
            };

            Func<List<TestObject>> ormLiteAction = () =>
            {
                var dbFactory = new ServiceStack.OrmLite.OrmLiteConnectionFactory(ConnectionString, ServiceStack.OrmLite.SqlServerDialect.Provider);
                using (System.Data.IDbConnection db = dbFactory.OpenDbConnection())
                {
                    var queryResults = ServiceStack.OrmLite.ReadConnectionExtensions.Select<TestObject>(db, q => q.Limit(LIMIT));
                    return queryResults.ToList();
                }
            };

            Func<List<TestObject>> entityFrameworkAction = () =>
            {
                using (var db = new MockObjectContext())
                {
                    var query = (from g in db.MockObjects select g).Take(LIMIT);
                    var efResults = query.ToList();
                    return efResults;
                }
            };

            Func<List<TestObject>> destrierAction = () =>
            {
                return new Destrier.Query<TestObject>().Limit(LIMIT).StreamResults().ToList();
            };

            Func<List<TestObject>> dapperAction = () =>
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
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
                { "ServiceStack ORMLite", ormLiteAction },
                { "Dapper", dapperAction },
                { "EntityFramework", entityFrameworkAction }
            };

            var results = new List<Int64>();

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
        }
    }
}
