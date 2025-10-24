using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;
using UAManagedCore;

namespace NETCode.Stations.Operations
{
    public class ValidateValueBehavior : IOperationBehavior
    {
        // === CONFIGURABLE TOLERANCE (10% por defecto) ===
        private const float FLOAT_TOLERANCE_PERCENT = 0.10f; // 10% tolerance

        public OperationResult Execute(Station_Base context, Operation operation)
        {
            try
            {
                // 1) Validate STRING type comparison
                if (!string.IsNullOrEmpty(operation.ValueString))
                {
                    string tag = $"To/Results/{operation.Index}/Result_STRING";
                    string actual = context.ReadStringTag(tag);

                    if (string.IsNullOrEmpty(actual))
                    {
                        Log.Info($"[{context._stationTag}] [ValidateValue] Waiting for string value in '{tag}'...");
                        return OperationResult.Waiting;
                    }

                    Log.Info($"[{context._stationTag}] [ValidateValue] Comparing expected '{operation.ValueString}' with actual '{actual}'");

                    return actual.Trim().Equals(operation.ValueString.Trim(), StringComparison.OrdinalIgnoreCase)
                        ? OperationResult.Passed
                        : OperationResult.Failed;
                }

                // 2) Validate REAL (float) type comparison with tolerance
                else if (operation.ValueReal.HasValue)
                {
                    string tag = $"To/Results/{operation.Index}/Result_REAL";
                    float actual = context.ReadFloatTag(tag) ?? float.NaN;

                    if (float.IsNaN(actual))
                    {
                        Log.Info($"[{context._stationTag}] [ValidateValue] Waiting for real value in '{tag}'...");
                        return OperationResult.Waiting;
                    }

                    float expected = operation.ValueReal.Value;
                    float tolerance = expected * FLOAT_TOLERANCE_PERCENT;
                    float minAcceptable = expected - tolerance;
                    float maxAcceptable = expected + tolerance;

                    Log.Info($"[{context._stationTag}] [ValidateValue] Expected: {expected}, Actual: {actual}, Tolerance ±{tolerance} ({FLOAT_TOLERANCE_PERCENT * 100:F0}%)");

                    return (actual >= minAcceptable && actual <= maxAcceptable)
                        ? OperationResult.Passed
                        : OperationResult.Failed;
                }

                // 3) Neither ValueString nor ValueReal defined → Waiting
                Log.Warning($"[{context._stationTag}] [ValidateValue] No expected value defined for operation {operation.ID}.");
                return OperationResult.Waiting;
            }
            catch (Exception ex)
            {
                Log.Error($"[{context._stationTag}] [ValidateValue] Error: {ex.Message}");
                return OperationResult.Failed;
            }
        }
    }
}
