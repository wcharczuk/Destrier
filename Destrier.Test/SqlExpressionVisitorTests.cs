using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Test
{
    public class SqlExpressionVisitorTests
    {
        [Fact]
        public void SqlExpressionVisitor()
        {
            //basic constant equality test
            var visitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, bool>> exp = (u) => u.MockObjectId == 1;
            visitor.Visit(exp);
            var sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] = @{0}", visitor.Parameters.First().Key));

            //FUN WITH BOOLEAN! 
            //SQL sucks and doesn't have a native boolean construct, so we have to expand Eval(true) and Eval(false) to Eval(1 = 1) and Eval(1 = 0)
            //Yes really.
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.Active;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, "[Active] = 1");

            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.Active == true; //this is also valid. because science.
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, "[Active] = (1 = 1)");
 
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => !u.Active;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString().Trim();
            Assert.Equal(sqlText, "NOT ([Active] = 1)");

            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId > 10 && 4 == (2 + 2);
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] > @{0} and (1 = 1)", visitor.Parameters.First().Key));

            //order shouldn't matter in equality
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => 1 == u.MockObjectId;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("@{0} = [MockObjectId]", visitor.Parameters.First().Key));

            //greater than
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId > 1;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] > @{0}", visitor.Parameters.First().Key));

            //greater or equal to
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId >= 1;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] >= @{0}", visitor.Parameters.First().Key));

            //less than
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId < 1;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] < @{0}", visitor.Parameters.First().Key));

            //less than or equal to
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId <= 1;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] <= @{0}", visitor.Parameters.First().Key));

            //boolean operator
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId > 1 && u.MockObjectId < 10;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();

            var pattern = @"\[MockObjectId\] > @(.*) and \[MockObjectId\] < @(.*)";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //boolean operator on variable
            int gt = 1;
            int lt = 10;
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId > gt && u.MockObjectId < lt;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();

            pattern = @"\[MockObjectId\] > @(.*) and \[MockObjectId\] < @(.*)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //member access
            var userAuth = new SubObject() { SubObjectId = 1 };
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.SubObjectId == userAuth.SubObjectId;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();
            pattern = @"\[SubObjectId\] = @(.*)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //member access with a cast
            var subObject = new SubObject() { SubObjectTypeId = SubObjectTypeId.Main };
            visitor = new SqlExpressionVisitor<MockObject>();
            exp = (u) => u.MockObjectId == (int)subObject.SubObjectTypeId;
            visitor.Visit(exp);
            sqlText = visitor.Buffer.ToString();

            pattern = @"\[MockObjectId\] = @(.*)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //dates!
            var date = DateTime.Now;
            var dateVisitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, bool>> dateExp = (a) => a.Created > date;
            dateVisitor.Visit(dateExp);
            sqlText = dateVisitor.Buffer.ToString();

            pattern = @"\[Created\] > @(.*)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //dates and function evaluation
            dateVisitor = new SqlExpressionVisitor<MockObject>();
            dateExp = (a) => a.Created > DateTime.Now.AddDays(-30);
            dateVisitor.Visit(dateExp);
            sqlText = dateVisitor.Buffer.ToString();

            pattern = @"\[Created\] > @(.*)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            //can evaluate unary operations
            var garmentSizeVisitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, bool>> gsExp = (gs) => gs.MockObjectId == 1 + 3;
            garmentSizeVisitor.Visit(gsExp);
            pattern = @"\[MockObjectId\] = @(.*)";
            sqlText = garmentSizeVisitor.Buffer.ToString();
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));
            Assert.True((int)garmentSizeVisitor.Parameters.Values.First() == 4);
        }

        [Fact]
        public void ListVisitor_Test()
        {
            var list = new List<Int32>() { 1, 2, 3, 4, 5 };
            var listVisitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, Boolean>> listExp = (mo) => list.Contains(mo.MockObjectId);
            listVisitor.Visit(listExp);

            var sqlText = listVisitor.Buffer.ToString();

            Assert.NotEmpty(sqlText);
            var pattern = @"\[MockObjectId\] IN \(1,2,3,4,5\)";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));

            listVisitor = new SqlExpressionVisitor<MockObject>();
            var stringList = new List<String>() { "hello", "hi", "--; DROP TABLE MOCKOBJECT" };
            Expression<Func<MockObject, Boolean>> stringListExp = (mo) => stringList.Contains(mo.MockObjectName);
            listVisitor.Visit(stringListExp);

            sqlText = listVisitor.Buffer.ToString();

            Assert.NotEmpty(sqlText);
            pattern = @"\[MockObjectName\] IN \((.*)\)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));


            listVisitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, Boolean>> listExpNonAccess = (mo) => mo.MockObjectId == 1 && list.Contains(5);
            listVisitor.Visit(listExpNonAccess);

            sqlText = listVisitor.Buffer.ToString();
            pattern = @"\[MockObjectId\] = @(.*) and \(1 = 1\)";
            regex = new System.Text.RegularExpressions.Regex(pattern);
            Assert.True(regex.IsMatch(sqlText));
        }

        [Fact]
        public void NullableVisitor_Tests()
        {
            var nullableVisitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, bool>> nullExp = (m) => m.MockObjectName == null;
            Expression<Func<MockObject, bool>> notNullExp = (m) => m.MockObjectName != null;
            nullableVisitor.Visit(nullExp);

            var sqlText = nullableVisitor.Buffer.ToString();
            Assert.Equal("[MockObjectName] is null", sqlText);

            nullableVisitor = new SqlExpressionVisitor<MockObject>();
            nullableVisitor.Visit(notNullExp);

            sqlText = nullableVisitor.Buffer.ToString();
            Assert.Equal("[MockObjectName] is not null", sqlText);

            Expression<Func<MockObject, bool>> nullExpCompound = (m) => m.Modified == null && m.Created > DateTime.Now.AddDays(-5);
            nullableVisitor = new SqlExpressionVisitor<MockObject>();
            nullableVisitor.Visit(nullExpCompound);
            sqlText = nullableVisitor.Buffer.ToString();

            Assert.Equal(String.Format("[Modified] is null and [Created] > @{0}", nullableVisitor.Parameters.First().Key), sqlText);
        }
    }
}
