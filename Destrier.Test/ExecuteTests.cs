using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test
{
    public class ExecuteTests : IUseFixture<TestObjectContext>
    {
        public void SetFixture(TestObjectContext data)
        { }

        [Fact]
        public void StoredProcedureReader_Test()
        {
            var hasRows = false;
            Execute.StoredProcedureReader("GetTestObjects_prc", (dr) =>
                {
                    hasRows = dr.HasRows;
                });

            Assert.True(hasRows);
        }

        [Fact]
        public void StoredProcedureReader_WithParameter_Test()
        {
            var objects = new List<TestObject>();
            Execute.StoredProcedureReader("GetTestObjects_prc", (dr) =>
            {
                objects = dr.ReadList<TestObject>();
            }, parameters: new { limit = 100 });

            Assert.NotEmpty(objects);
            Assert.True(objects.Count == 100);
        }

        [Fact]
        public void NonQuery_Test()
        {
            Execute.NonQuery("update TestObjects set name = 'name_zero' where id = 1");

            String newName = String.Empty;
            Execute.StatementReader("select name from testobjects where id = 1", (dr) =>
                {
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            newName = dr.Get<String>(0);
                        }
                    }
                });

            Assert.Equal("name_zero", newName);
        }

        [Fact]
        public void NonQuery_WithParam_Test()
        {
            Execute.NonQuery("update TestObjects set name = @name where id = @id", new { name = "name_zero", id = 1 });

            String newName = String.Empty;
            Execute.StatementReader("select name from testobjects where id = 1", (dr) =>
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        newName = dr.Get<String>(0);
                    }
                }
            });

            Assert.Equal("name_zero", newName);
        }

        [Fact]
        public void StatementReader_WithParameter()
        {
            String newName = String.Empty;
            Execute.StatementReader("select name from testobjects where id = @id", (dr) =>
            {
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        newName = dr.Get<String>(0);
                    }
                }
            }, parameters: new { id = 1 });

            Assert.NotNull(newName);
            Assert.NotEmpty(newName);
        }

        [Fact]
        public void Utility_AddWhereClauseVariables()
        {
            StringBuilder builder = new StringBuilder();
            var parameters = new { name = "test" };
            Execute.Utility.AddWhereClauseVariables(parameters, builder);
            var text = builder.ToString();

            Assert.NotEmpty(text);
            Assert.Equal("and [name] = @name\r\n", text);
        }
    }
}
