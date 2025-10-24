using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;
using UAManagedCore;

public class WaitForCompleteBehavior : IOperationBehavior
{
    private const int PASS_STATUS = 1; // <-- antes era 0

    public OperationResult Execute(Station_Base context, Operation operation)
    {
        try
        {
            int i = operation.Index;
            string completeTag = $"To/Results/{i}/Complete";
            string statusTag = $"To/Results/{i}/Status";

            bool isComplete = SafeReadComplete(context, completeTag);
            if (!isComplete)
                return OperationResult.Waiting;

            int status = SafeReadInt(context, statusTag, defaultValue: 2); // 2 = NOK/Timeout
            bool passed = (status == PASS_STATUS);

            Log.Info($"[WaitForComplete] OpIdx={i} Complete=TRUE Status={status} => {(passed ? "PASSED" : "FAILED")}");
            return passed ? OperationResult.Passed : OperationResult.Failed;
        }
        catch (Exception ex)
        {
            Log.Error($"[WaitForComplete] Error: {ex.Message}");
            return OperationResult.Waiting;
        }
    }

    private static bool SafeReadComplete(Station_Base ctx, string relPath)
    {
        try { return ctx.ReadBoolTag(relPath); }
        catch { try { return (ctx.ReadIntTag(relPath) ?? 0) != 0; } catch { return false; } }
    }

    private static int SafeReadInt(Station_Base ctx, string relPath, int defaultValue)
    {
        try { return ctx.ReadIntTag(relPath) ?? defaultValue; }
        catch { return defaultValue; }
    }
}
