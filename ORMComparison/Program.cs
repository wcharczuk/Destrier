using System;
using System.Collections.Generic;
using System.Data;
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
            this.Property(t => t.NullName);
            this.Property(t => t.Active);
            this.Property(t => t.Created);
            this.Property(t => t.Modified);
            this.Property(t => t.ReferencedObjectId);
            this.Property(t => t.NullableId);
            //this.Property(t => t.Type);
            //this.Property(t => t.NullableType);
            this.Property(t => t.SingleChar);
            //this.Property(t => t.Single);
            this.Property(t => t.Double);
            this.Property(t => t.NullableDouble);
            this.Property(t => t.Guid);
            this.Property(t => t.NullableGuid);
            this.ToTable("TestObjects");
        }
    }

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
		public const String ConnectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=True";
        public const int TRIALS = 100;
        public const int LIMIT = 5000;

        static void SetValuesForObject(IDataReader dr, object instance)
        {
            ((TestObject)instance).Id = dr.GetInt32(0);
            ((TestObject)instance).Name = dr.GetString(1);
            ((TestObject)instance).Name = !dr.IsDBNull(2) ? dr.GetString(2) : null;
            ((TestObject)instance).Active = dr.GetBoolean(3);
            ((TestObject)instance).Created = dr.GetDateTime(4);
            ((TestObject)instance).Modified = !dr.IsDBNull(5) ? (DateTime?)dr.GetDateTime(5) : null;
            ((TestObject)instance).NullableId = !dr.IsDBNull(6) ? (int?)dr.GetInt32(6) : null;
            ((TestObject)instance).ReferencedObjectId = !dr.IsDBNull(7) ? dr.GetInt32(7) : default(Int32);
            ((TestObject)instance).Type = (TestObjectTypeId)dr.GetInt32(8);
            ((TestObject)instance).NullableType = !dr.IsDBNull(9) ? (Int32?)dr.GetInt32(9) : null;
            ((TestObject)instance).SingleChar = dr.GetString(10);
            ((TestObject)instance).Single = (Single)dr.GetDouble(11);
            ((TestObject)instance).Double = !dr.IsDBNull(12) ? dr.GetDouble(12) : default(Double);
            ((TestObject)instance).NullableDouble = !dr.IsDBNull(13) ? (Double?)dr.GetDouble(13) : null;
            ((TestObject)instance).Guid = !dr.IsDBNull(14) ? dr.GetGuid(14) : default(Guid);
            ((TestObject)instance).NullableGuid = !dr.IsDBNull(15) ? (Guid?)dr.GetGuid(15) : null;
        }

        static void Main(string[] args)
        {
            Destrier.DatabaseConfigurationContext.ConnectionStrings.Add("default", ConnectionString);
            Destrier.DatabaseConfigurationContext.DefaultConnectionName = "default";
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";

            var testObjectContext = new Destrier.Test.TestObjectContext();

            string QUERY = String.Format(@"SELECT TOP {0} 
Id
, Name
, NullName
, Active
, Created
, Modified
, NullableId
, ReferencedObjectId
, Type
, NullableType
, SingleChar
, [Single]
, [Double]
, [NullableDouble]
, [Guid]
, [NullableGuid] 
from TestObjects (nolock)", LIMIT);

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
                                    var testObject = Destrier.ReflectionCache.GetNewObject<TestObject>();
                                    SetValuesForObject(dr, testObject);
                                    list.Add(testObject);
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
                    return ServiceStack.OrmLite.ReadConnectionExtensions.Select<TestObject>(db, q => q.Limit(LIMIT));
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

            Func<List<TestObject>> destrierRawQueryAction = () =>
            {
                return new Destrier.Query<TestObject>(QUERY).StreamResults().ToList();
            };

            Func<List<TestObject>> petaPocoAction = () =>
                {
                    var db = new PetaPoco.Database(ConnectionString, "System.Data.SqlClient");
                    return db.Query<TestObject>(QUERY).ToList();
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
                { "Destrier (Raw Query)", destrierRawQueryAction },
                { "PetaPoco", petaPocoAction },
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

            testObjectContext.EnsureDestroyDataStore();
        }
    }
}
