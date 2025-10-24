using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;

namespace NETCode.Stations.Operations;

public class CaptureResultBehavior : IOperationBehavior
{
    public OperationResult Execute(Station_Base context, Operation operation)
    {
        string tag = operation.ValueReal.HasValue
            ? $"To/Results/{operation.Index}/Result_REAL"
            : $"To/Results/{operation.Index}/Result_STRING";

        object actual = context.ReadGenericTag(tag);

        // Check if the value is not null or empty (for strings) or has any numeric value
        if ((actual is string s && !string.IsNullOrEmpty(s)) ||
            (actual is float f))
        {
            return OperationResult.Passed;
        }

        return OperationResult.Waiting;
    }
}
