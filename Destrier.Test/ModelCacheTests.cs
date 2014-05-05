using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Test
{
    public class ModelCacheTests
    {
        [Fact]
        public void GenerateMembersRecursive_Test()
        {
            var memberList = Model.GenerateAllMembers(typeof(Mapping));
            Assert.NotNull(memberList);
            Assert.NotEmpty(memberList);

            Assert.True(memberList.Any(m => m is ChildCollectionMember));
            Assert.True(memberList.Any(m => m is ReferencedObjectMember));

            var childCollectionMembers = memberList.Where(m => m is ChildCollectionMember);
            childCollectionMembers.All(cm => cm.Parent != null);

            var referencedObjectMembers = memberList.Where(m => m is ReferencedObjectMember);
            referencedObjectMembers.All(cm => cm.Parent != null);
        }
    }
}
