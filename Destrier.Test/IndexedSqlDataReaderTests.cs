using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test
{
    public class IndexedSqlDataReaderTests : IUseFixture<TestObjectContext>
    {
        public static String ManyStatment = "select top 100 * from testobjects";
        public static String SingleStatment = "select top 1 * from testobjects";

        public void SetFixture(TestObjectContext data) { }

        [Fact]
        public void InitializeTest_WithType_Test()
        {
            using (var cmd = Execute.Command(DatabaseConfigurationContext.DefaultConnectionString))
            {
                cmd.CommandText = SingleStatment;
                cmd.CommandType = System.Data.CommandType.Text;
                using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader(), type: typeof(TestObject), standardizeCasing: false))
                {
                }
            }
        }
        
        [Fact]
        public void Metadata_Test()
        {
            using (var cmd = Execute.Command(DatabaseConfigurationContext.DefaultConnectionString))
            {
                cmd.CommandText = SingleStatment;
                cmd.CommandType = System.Data.CommandType.Text;
                using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader(), type: typeof(TestObject), standardizeCasing: false))
                {
                    dr.ReadFullControl((reader) =>
                    {
                        Assert.True(reader.HasColumn("id"));
                        Assert.True(reader.GetColumnIndex("id") == reader.GetOrdinal("id"));
                        Assert.False(reader.IsClosed);
                    });

                    DataException de = null;
                    try
                    {
                        IndexedSqlDataReader.ThrowDataException(new Exception("Test Exception"), 0, dr);
                    }
                    catch (DataException e)
                    {
                        de = e;
                    }

                    Assert.NotNull(de);
                }
            }


        }

        [Fact]
        public void InitializeTest_NoType_Test()
        {
            using (var cmd = Execute.Command(DatabaseConfigurationContext.DefaultConnectionString))
            {
                cmd.CommandText = SingleStatment;
                cmd.CommandType = System.Data.CommandType.Text;
                using (var dr = new IndexedSqlDataReader(cmd.ExecuteReader()))
                {

                }
            }
        }

        [Fact]
        public void ReadDynamic_Test()
        {
            dynamic myObj = null;
            Execute.StatementReader(SingleStatment, (dr) =>
            {
                myObj = dr.ReadDynamic();
            });

            Assert.NotNull(myObj);
            Assert.NotNull(myObj.name);
        }

        [Fact]
        public void ReadDynamicList_Test()
        {
            List<dynamic> objects = null;
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                objects = dr.ReadDynamicList();
            });

            Assert.NotNull(objects);
            Assert.NotEmpty(objects);
            Assert.NotNull(objects.First().name);
        }

        [Fact]
        public void ReadScalar_Test()
        {
            int? myObj = null;
            Execute.StatementReader(SingleStatment, (dr) =>
            {
                myObj = dr.ReadScalar<int>();
            });

            Assert.NotNull(myObj);
        }

        [Fact]
        public void ReadObject_Test()
        {
            TestObject myObj = null;
            Execute.StatementReader(SingleStatment, (dr) =>
            {
                myObj = dr.ReadObject<TestObject>();
            });

            Assert.NotNull(myObj);
            Assert.NotNull(myObj.Name);
        }

        [Fact]
        public void ReadScalarList_Test()
        {
            List<int> myObj = null;
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                myObj = dr.ReadScalarList<int>();
            });

            Assert.NotNull(myObj);
            Assert.NotEmpty(myObj);
            Assert.True(myObj.Last() > 1);
        }

        [Fact]
        public void ReadList_Test()
        {
            List<TestObject> myObj = null;
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                myObj = dr.ReadList<TestObject>();
            });

            Assert.NotNull(myObj);
            Assert.NotEmpty(myObj);
            Assert.True(myObj.Last().Id > 1);
        }

        [Fact]
        public void ReadDictionary_Test()
        {
            Dictionary<Int32, TestObject> myObj = null;
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                myObj = dr.ReadDictionary<Int32, TestObject>(to => to.Id);
            });

            Assert.NotNull(myObj);
            Assert.NotEmpty(myObj);
            Assert.True(myObj.Last().Key > 1);
            Assert.True(myObj.Last().Value.Id > 1);
        }

        [Fact]
        public void ReadIntoParentCollection_Test()
        {
            Dictionary<Int32, TestObject> myObj = new Dictionary<int, TestObject>();
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                dr.ReadIntoParentCollection<TestObject>((reader, to) =>
                {
                    myObj.Add(to.Id, to);
                });
            });

            Assert.NotNull(myObj);
            Assert.NotEmpty(myObj);
            Assert.True(myObj.Last().Key > 1);
            Assert.True(myObj.Last().Value.Id > 1);
        }

        [Fact]
        public void ReadFullControl_Test()
        {
            Dictionary<Int32, TestObject> myObj = new Dictionary<int, TestObject>();
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                dr.ReadFullControl((reader) =>
                {
                    var to = new TestObject();
                    Model.Populate(to, reader);
                    myObj.Add(to.Id, to);
                });
            });

            Assert.NotNull(myObj);
            Assert.NotEmpty(myObj);
            Assert.True(myObj.Last().Key > 1);
            Assert.True(myObj.Last().Value.Id > 1);
        }

        [Fact]
        public void Get_T_Test()
        {
            Execute.StatementReader(ManyStatment, (dr) =>
            {
                dr.ReadFullControl((reader) =>
                {
                    var testObject = new TestObject();
                    testObject.Id = dr.Get<Int32>("id");
                    testObject.Name = dr.Get<String>("name");
                    testObject.NullName = dr.Get<String>("nullname");
                    testObject.Created = dr.Get<DateTime>("created");
                    testObject.Modified = dr.Get<DateTime?>("modified");
                    testObject.Type = dr.Get<TestObjectTypeId>("type");
                    testObject.NullableType = dr.Get<TestObjectTypeId?>("nullableType");
                    testObject.SingleChar = dr.Get<String>("singleChar");
                    testObject.Single = dr.Get<Single>("single");
                    testObject.Double = dr.Get<Double>("double");
                    testObject.NullableDouble = dr.Get<Double?>("nullableDouble");
                    testObject.Guid = dr.Get<Guid>("guid");
                    testObject.NullableGuid = dr.Get<Guid?>("nullableGuid");
                });
            });
        }

        [Fact]
        public void FastPipelineTest()
        {
            var testObjects = new Query<TestObject>().Limit(100).Execute();

            Assert.NotNull(testObjects);
            Assert.NotEmpty(testObjects);
        }
    }
}
