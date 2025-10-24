using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Stations.Operations;

public class SendCommandBehavior : IOperationBehavior
{
    public OperationResult Execute(Station_Base context, Operation operation)
    {
        context.WriteSingleTag("From/Operation_Index", operation.Index + 1);
        return OperationResult.Passed;
    }
}
