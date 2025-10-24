
using System;
using System.Collections.Generic;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Repositories;
using NETCode.Stations;
using UAManagedCore; // Log.Info / Log.Error

namespace NETCode.Stations.Operations
{
    /// <summary>
    /// - Pulses trigger after writing payload & count
    /// - Waits Complete; Status == PASS_STATUS => Passed
    /// </summary>
    public class SendListAndWaitOkBehavior : IOperationBehavior
    {
        // === CONFIG ===
        private const bool FORCE_PTL = true;
        private const int PTL_TYPE_ID = 7;
        private const int PASS_STATUS = 1;

        // From/Operations/{i}/...
        private static string FromValueString(int i) => $"From/Operations/{i}/Value_STRING";
        private static string FromValueReal(int i) => $"From/Operations/{i}/Value_REAL";

        // To/Results/{i}/...
        private static string ToComplete(int i) => $"To/Results/{i}/Complete";
        private static string ToStatus(int i) => $"To/Results/{i}/Status";

        public OperationResult Execute(Station_Base ctx, Operation op)
        {
            if (ctx == null || op == null)
                return OperationResult.Failed;

            // ---- SEND PHASE (every scan) ----
            try
            {
                if (op.OperationTypeID == PTL_TYPE_ID)
                {
                    SendToPickToLight(ctx, op);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[{ctx._stationTag}] [PTL] Send phase error: {ex.Message}");
                return OperationResult.Failed;
            }

            // ---- WAIT PHASE ----
            bool complete = SafeReadBool(ctx, ToComplete(op.Index));
            if (!complete)
                return OperationResult.Waiting;

            int status = SafeReadInt(ctx, ToStatus(op.Index), int.MinValue);
            bool passed = (status == PASS_STATUS);

            Log.Info($"[{ctx._stationTag}] [PTL] Complete=true, Status={status} => {(passed ? "PASSED" : "FAILED")} (OpIdx={op.Index})");
            return passed ? OperationResult.Passed : OperationResult.Failed;
        }

        // ============== FULL PTL SEND ==============
        private static void SendToPickToLight(Station_Base ctx, Operation op)
        {
            // 1) Parse part numbers from operation.ValueString
            var parts = ParsePartNumbers(op.ValueString);
            if (parts.Count == 0)
            {
                Log.Info($"[{ctx._stationTag}] [PTL] No part numbers for OpIdx={op.Index}");
                return;
            }

            // 2) Resolve bin positions from DB
            var repo = new PickToLightBinRepository();
            var bins = repo.GetBinPositionsForParts(parts);
            if (bins == null || bins.Count == 0)
            {
                Log.Info($"[{ctx._stationTag}] [PTL] No bins for parts: {string.Join(",", parts)}");
                return;
            }

            string payload = string.Join(",", bins);

            // 3) Write payload & count (every scan by request)
            string sPath = FromValueString(op.Index);
            string rPath = FromValueReal(op.Index);

            SafeWriteString(ctx, sPath, payload);
            SafeWriteInt(ctx, rPath, bins.Count);

            Log.Info($"[{ctx._stationTag}] [PTL] Wrote payload='{payload}', count={bins.Count} (OpIdx={op.Index})");
        }

        // ============== SAFE I/O HELPERS ==============
        private static bool SafeReadBool(Station_Base ctx, string relPath)
        {
            try { return ctx.ReadBoolTag(relPath); }
            catch { return false; }
        }

        private static int SafeReadInt(Station_Base ctx, string relPath, int defVal)
        {
            try { return (int)ctx.ReadIntTag(relPath); }
            catch { return defVal; }
        }

        private static void SafeWriteInt(Station_Base ctx, string relPath, int value)
        {
            try { ctx.WriteSingleTag(relPath, value); }
            catch { }
        }

        private static void SafeWriteString(Station_Base ctx, string relPath, string value)
        {
            try { ctx.WriteSingleTag(relPath, value); }
            catch { }
        }

        private static List<string> ParsePartNumbers(string valueString)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(valueString))
                return list;

            foreach (var token in valueString.Split(','))
            {
                var p = token?.Trim();
                if (!string.IsNullOrEmpty(p))
                    list.Add(p);
            }

            return list;
        }
    }
}

