using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class RootMember : Member    
    {
        public RootMember() : base() { }
        
        public RootMember(Type type) : base(type) 
        {
            this.TableAlias = Model.GenerateTableAlias();
        }

        public RootMember(Member member) : base(member)  { }
    }
}
