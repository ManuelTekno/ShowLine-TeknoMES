
using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;


namespace NETCode.Stations.Operations;

// DisplayOnlyBehavior.cs
public class DisplayOnlyBehavior : IOperationBehavior
{
    private readonly Dictionary<int, DateTime> _startTimes = new();

    public OperationResult Execute(Station_Base context, Operation operation)
    {
        if (!_startTimes.ContainsKey(operation.Index))
            _startTimes[operation.Index] = DateTime.Now;

        int duration = operation.ValueReal.HasValue ? (int)operation.ValueReal.Value : 1000;
        var elapsed = DateTime.Now - _startTimes[operation.Index];

        if (elapsed.TotalMilliseconds >= duration)
        {
            _startTimes.Remove(operation.Index);
            context.WriteSingleTag("From/Operation_Index", operation.Index + 1);
            return OperationResult.Waiting;
        }
        return OperationResult.Passed;
    }
}
