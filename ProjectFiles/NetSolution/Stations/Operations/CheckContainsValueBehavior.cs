using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;

namespace NETCode.Stations.Operations;

public class CheckContainsValueBehavior : IOperationBehavior
{
    public OperationResult Execute(Station_Base context, Operation operation)
    {
        if (string.IsNullOrEmpty(operation.ValueString))
            return OperationResult.Waiting;

        string tag = $"To/Results/{operation.Index}/Result_STRING";
        object actual = context.ReadGenericTag(tag);

        if (actual is string actualString && actualString.Contains(operation.ValueString))
        {
            return OperationResult.Passed;
        }
        return OperationResult.Failed;
    }
}
