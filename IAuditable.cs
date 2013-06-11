using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface IAuditable
    {
        List<DatabaseAction> AuditActions { get; }
        string AuditInstanceName { get; }
        object AuditGetPrevious(object current);
        void DoAudit(DatabaseAction action, object current, object previous = null);
    }
}