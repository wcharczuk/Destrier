using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using Dapper;

namespace ORMComparison
{
    public class GarmentContext : DbContext
    {
        public GarmentContext() : base(String.Format("Server={0};Database={1};Trusted_Connection=true", "localhost", "clotheshorse")) { }

        public DbSet<Garment> Garments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new GarmentMap());
        }
    }

    public class GarmentMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<Garment>
    {
        public GarmentMap()
        {
            this.HasKey(t => t.GarmentId);
            this.Property(t => t.GarmentTypeId);
            this.Property(t => t.BrandId);
            this.Property(t => t.GenderTypeId);
            this.Property(t => t.Active);
            this.Property(t => t.Season);
            this.Property(t => t.PricePoint);
            this.Property(t => t.Notes);
            this.Property(t => t.CreatedBy);
            this.Property(t => t.ModifiedBy);
            this.ToTable("Garment_tbl");
        }
    }

    [Destrier.Table("Garment_tbl")]
    [ServiceStack.DataAnnotations.Alias("Garment_tbl")]
    public class Garment : Destrier.BaseModel
    {
        [Destrier.Column(IsPrimaryKey=true)]
        [ServiceStack.DataAnnotations.PrimaryKey]
        public Int32 GarmentId { get; set; }
        [Destrier.Column]
        public Int16 GarmentTypeId { get; set; }
        [Destrier.Column]
        public Int32 BrandId { get; set; }
        [Destrier.Column]
        public Int16 GenderTypeId { get; set; }
        [Destrier.Column]
        public Boolean Active { get; set; }
        [Destrier.Column]
        public String Season { get; set; }
        [Destrier.Column]
        public String PricePoint { get; set; }
        [Destrier.Column]
        public String Notes { get; set; }
        [Destrier.Column]
        public int? CreatedBy { get; set; }
        [Destrier.Column]
        public int? ModifiedBy { get; set; }   
    }

    

    public class Program
    {
        public const String ConnectionString = "Server=localhost;Database=clotheshorse;Trusted_Connection=true";
        public const int TRIALS = 1;
        public const int LIMIT = 2000;

        public static void Main(string[] args)
        {
            Destrier.DatabaseConfigurationContext.ConnectionStrings.Add("default", ConnectionString);
            Destrier.DatabaseConfigurationContext.DefaultConnectionName = "default";
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "ClothesHorse";

            var dbFactory = new ServiceStack.OrmLite.OrmLiteConnectionFactory(ConnectionString, ServiceStack.OrmLite.SqlServerDialect.Provider);

            var results = new List<Int64>();

            Action rawAction = () =>
            {
                var list = new List<Garment>();
                using (var conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;

                        cmd.CommandText = String.Format("SELECT TOP {0} GarmentId, GenderTypeId, GarmentTypeId, Active, BrandId, Notes, SourceTypeId, CreatedBy, ModifiedBy, PricePoint, Season from Garment_tbl (nolock)", LIMIT);
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (var dr = cmd.ExecuteReader(System.Data.CommandBehavior.Default))
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    var garment = Destrier.ReflectionCache.GetNewObject<Garment>();
                                    garment.GarmentId = dr.GetInt32(0);
                                    garment.GenderTypeId = dr.GetInt16(1);
                                    garment.GarmentTypeId = dr.GetInt16(2);
                                    garment.Active = dr.GetBoolean(3);
                                    garment.BrandId = dr.GetInt32(4);
                                    garment.Notes = !dr.IsDBNull(5) ? dr.GetString(5) : String.Empty;
                                    garment.CreatedBy = !dr.IsDBNull(7) ? dr.GetInt32(7) : default(int);
                                    garment.ModifiedBy = !dr.IsDBNull(8) ? dr.GetInt32(8) : default(int);
                                    garment.PricePoint = !dr.IsDBNull(9) ? dr.GetString(9) : String.Empty;
                                    garment.Season = !dr.IsDBNull(10) ? dr.GetString(10) : String.Empty;
                                }
                            }
                        }
                    }
                }
            };

            Action ormLiteAction = () =>
            {
                using (System.Data.IDbConnection db = dbFactory.OpenDbConnection())
                {
                    var garments = db.Select<Garment>(q => q.Limit(LIMIT));
                }
            };

            Action entityFrameworkAction = () =>
            {
                using (var db = new GarmentContext())
                {
                    var query = (from g in db.Garments select g).Take(LIMIT);
                    var efResults = query.ToList();
                }
            };

            Action destrierAction = () =>
            {
                var destrierResults = new Destrier.Query<Garment>().Limit(LIMIT).Execute();
            };

            Action dapperAction = () =>
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    var dapperResults = conn.Query<Garment>(String.Format("SELECT TOP {0} GarmentId, GenderTypeId, GarmentTypeId, Active, BrandId, Notes, SourceTypeId, CreatedBy, ModifiedBy, PricePoint, Season from Garment_tbl (nolock)", LIMIT));
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

            Dictionary<String, Action> testSteps = new Dictionary<string, Action>()
            {
                { "Raw Reader", rawAction },
                { "Destrier", destrierAction },
                //{ "ServiceStack ORMLite", ormLiteAction },
                //{ "Dapper", dapperAction },
                { "EntityFramework", entityFrameworkAction }
            };

            foreach (var kvp in testSteps)
            {
                results = new List<long>();
                for (int x = 0; x < TRIALS; x++)
                {
                    stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    kvp.Value();
                    stopwatch.Stop();
                    results.Add(stopwatch.ElapsedMilliseconds);
                }
                
                Console.Write(kvp.Key);
                int spaces = 25 - kvp.Key.Length;
                for (int x = 0; x < spaces; x++)
                    Console.Write(" ");

                Console.WriteLine(String.Format("Avg: {0}ms", results.Average()));
            }
            Console.WriteLine();
            Console.ReadKey();
        }
    }
}
