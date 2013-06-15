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

        public DbSet<MockObject> MockObjects { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new MockObjectMap());
        }
    }

    public class MockObjectMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<MockObject>
    {
        public MockObjectMap()
        {
            this.HasKey(t => t.Id);
            this.Property(t => t.Id);
            this.Property(t => t.Name);
            this.Property(t => t.Active);
            this.Property(t => t.Created);
            this.Property(t => t.Modified);
            this.Property(t => t.ReferencedObjectId);
            this.Property(t => t.NullableId);
            this.ToTable("MockObjects");
        }
    }

    [Destrier.Table("MockObjects")]
    [ServiceStack.DataAnnotations.Alias("MockObjects")]
    public class MockObject : Destrier.BaseModel
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
        public const String ConnectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
        public const int TRIALS = 1000;
        public const int LIMIT = 5000;

        public static void EnsureInitDataStore()
        {
            var initDbScript = @"
if (OBJECT_ID('tempdb..MockObjects') is not null)
BEGIN
    DROP TABLE MockObjects
END

CREATE TABLE MockObjects
( 
    id int not null identity(1,1) primary key, 
    name varchar(255) not null,
    mockObjectTypeId smallint not null, 
    active bit not null,
    created datetime not null,
    modified datetime,
    nullableId int,
    referencedObjectId int,
);

DECLARE @id int;
DECLARE @i int;
DECLARE @subId int;
DECLARE @typeId smallint;

SET @i = 0;
SET @subId = 1;
SET @typeId = 1;

WHILE @i < 5001
BEGIN
    INSERT INTO MockObjects ([Name], [mockObjectTypeId], [active], [created], [modified], [nullableId], [referencedObjectId]) VALUES ( 'name' + cast(@i as varchar), @typeId, 1, getdate(), null, null, @subId);

    IF(@subId = 100) BEGIN; SET @subId = 1; END;
    IF(@typeId = 10) BEGIN; SET @typeId = 1; END;

    SET @subId = @subId + 1;
    SET @typeId = @typeId + 1;
    SET @i = @i + 1;
END

";
            Destrier.Execute.NonQuery(initDbScript);
        }

        public static void Main(string[] args)
        {
            Destrier.DatabaseConfigurationContext.ConnectionStrings.Add("default", ConnectionString);
            Destrier.DatabaseConfigurationContext.DefaultConnectionName = "default";
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";

            EnsureInitDataStore();

            string QUERY = String.Format("SELECT TOP {0} Id, Name, Active, MockObjectTypeId, Created, Modified, NullableId, ReferencedObjectId from MockObjects (nolock)", LIMIT);

            Func<List<MockObject>> rawAction = () =>
            {
                var list = new List<MockObject>();
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
                                    var mockObject = Destrier.ReflectionCache.GetNewObject<MockObject>();
                                    mockObject.Id = dr.GetInt32(0);
                                    mockObject.Name = dr.GetString(1);
                                    mockObject.Active = dr.GetBoolean(2);
                                    mockObject.Created = dr.GetDateTime(4);
                                    mockObject.Modified = !dr.IsDBNull(5) ? (DateTime?)dr.GetDateTime(5) : null;
                                    mockObject.NullableId = !dr.IsDBNull(6) ? (int?)dr.GetInt32(6) : null;
                                    mockObject.ReferencedObjectId = dr.GetInt32(7);

                                    list.Add(mockObject);
                                }
                            }
                        }
                    }
                }
                return list;
            };

            Func<List<MockObject>> ormLiteAction = () =>
            {
                var dbFactory = new ServiceStack.OrmLite.OrmLiteConnectionFactory(ConnectionString, ServiceStack.OrmLite.SqlServerDialect.Provider);
                using (System.Data.IDbConnection db = dbFactory.OpenDbConnection())
                {
                    var queryResults = ServiceStack.OrmLite.ReadConnectionExtensions.Select<MockObject>(db, q => q.Limit(LIMIT));
                    return queryResults.ToList();
                }
            };

            Func<List<MockObject>> entityFrameworkAction = () =>
            {
                using (var db = new MockObjectContext())
                {
                    var query = (from g in db.MockObjects select g).Take(LIMIT);
                    var efResults = query.ToList();
                    return efResults;
                }
            };

            Func<List<MockObject>> destrierAction = () =>
            {
                return new Destrier.Query<MockObject>().Limit(LIMIT).StreamResults().ToList();
            };

            var destrierRawQuery = new Destrier.Query<MockObject>().Limit(LIMIT);
            Func<List<MockObject>> destrierReuseAction = () =>
            {
                return destrierRawQuery.StreamResults().ToList();
            };

            Func<List<MockObject>> destrierRawAction = () =>
            {
                return new Destrier.Query<MockObject>(QUERY).StreamResults().ToList();
            };

            Func<List<MockObject>> dapperAction = () =>
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    return conn.Query<MockObject>(QUERY).ToList();
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

            var testSteps = new Dictionary<string, Func<List<MockObject>>>()
            {
                { "Raw Reader", rawAction },
                { "Destrier", destrierAction },
                { "Destrier (Re-use Query)", destrierReuseAction },
                { "Destrier (Raw Query)", destrierReuseAction },
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
