using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FTOptix.HMIProject;
using NETCode.Entities;
using NETCode.Repositories;
using UAManagedCore;

namespace NETCode.Stations
{
    /// <summary>
    /// Tag-driven, raw Pick-To-Light broadcaster (no station_id, global queue).
    /// Drive it by calling Tick() periodically.
    ///
    /// PLC ↔ MES tags (relative to _plcNodePath):
    /// - PLC → MES:  RequestNext_BOOL    (TRUE when PLC wants the next PTL payload)
    /// - MES → PLC:  WriteBins_STRING    (comma-separated bin indexes, e.g., "1,3")
    /// - PLC → MES:  Complete_BOOL       (TRUE when operator finished picking)
    /// - MES → PLC:  Ack_BOOL            (optional pulse so PLC can reset its BOOLs)
    /// </summary>
    public class PickToLightBroadcasterRaw
    {
        private readonly string _plcNodePath;          // e.g., "Model/CommDrivers/AB/ShowLine/Tags/Controller Tags/STP110_Station"
        private readonly string _writeTagPath;         // e.g., "PTL/WriteBins_STRING"
        private readonly string _requestTagPath;       // e.g., "PTL/RequestNext_BOOL"
        private readonly string _completeTagPath;      // e.g., "PTL/Complete_BOOL"
        private readonly string _ackTagPath;           // e.g., "PTL/Ack_BOOL" (optional)
        private readonly bool _useAck;

        private readonly PickToLightScheduleRepository _scheduleRepo;
        private readonly PickToLightBinRepository _binRepo;

        // Tracks the currently-dispatched schedule row (awaiting Complete)
        private int _lastDispatchedScheduleId = -1;

        public PickToLightBroadcasterRaw(
            string plcNodePath,
            string writeTagRelativePath,
            string requestNextTagRelativePath,
            string completeTagRelativePath,
            string ackTagRelativePath = null)
        {
            _plcNodePath = plcNodePath ?? throw new ArgumentNullException(nameof(plcNodePath));
            _writeTagPath = writeTagRelativePath ?? throw new ArgumentNullException(nameof(writeTagRelativePath));
            _requestTagPath = requestNextTagRelativePath ?? throw new ArgumentNullException(nameof(requestNextTagRelativePath));
            _completeTagPath = completeTagRelativePath ?? throw new ArgumentNullException(nameof(completeTagRelativePath));
            _ackTagPath = ackTagRelativePath;
            _useAck = !string.IsNullOrWhiteSpace(_ackTagPath);

            _scheduleRepo = new PickToLightScheduleRepository(); // global queue repo (no station_id)
            _binRepo = new PickToLightBinRepository();
        }

        /// <summary>
        /// Call this on a periodic timer.
        /// - If RequestNext == TRUE and nothing is in-flight → dispatch next pending.
        /// - If Complete == TRUE and something is in-flight → mark completed (+ optional ACK pulse).
        /// </summary>
        public void Tick()
        {
            try
            {
                // 1) Handle "Complete" first (close out an in-flight job)
                TryCompleteIfDone();

                // 2) Handle "Request next" (dispatch a new job if none in-flight)
                TryDispatchIfRequested();
            }
            catch (Exception ex)
            {
                Log.Error($"[PTL-Raw] Tick() error: {ex.Message}");
            }
        }

        // ================= Dispatch & Complete =================

        private void TryDispatchIfRequested()
        {
            bool requested = ReadBool(_requestTagPath) == true;
            if (!requested)
                return;

            // Avoid double-dispatch: only dispatch if nothing is awaiting completion
            if (_lastDispatchedScheduleId > 0)
                return;

            // Fetch next pending; if none, try to reset the cycle once
            var next = _scheduleRepo.GetNextPending();
            if (next == null)
            {
                if (_scheduleRepo.CountPending() == 0)
                {
                    _scheduleRepo.ResetAllCompletedToPending();
                    next = _scheduleRepo.GetNextPending();
                }

                if (next == null)
                {
                    Log.Info("[PTL-Raw] No pending rows to dispatch.");
                    PulseAckIfEnabled(); // optional: acknowledge to let PLC clear request
                    return;
                }
            }

            // Resolve bins from payload_csv (supports either bins or part numbers)
            var tokens = SplitCsv(next.PayloadCsv);
            var bins = ResolveBins(tokens);
            if (bins.Count == 0)
            {
                Log.Warning($"[PTL-Raw] Invalid/empty bin list for schedule {next.Id} (payload='{next.PayloadCsv}').");
                PulseAckIfEnabled(); // prevent PLC from waiting forever
                return;
            }

            string payload = string.Join(",", bins);
            if (!TryWriteString(_writeTagPath, payload))
            {
                Log.Error($"[PTL-Raw] Failed to write payload for schedule {next.Id}");
                return;
            }

            _lastDispatchedScheduleId = next.Id;
            Log.Info($"[PTL-Raw] Dispatched schedule {next.Id}. Payload='{payload}'");

            // Let PLC reset its RequestNext (optional but common handshake)
            PulseAckIfEnabled();
        }

        private void TryCompleteIfDone()
        {
            if (_lastDispatchedScheduleId <= 0)
                return;

            bool done = ReadBool(_completeTagPath) == true;
            if (!done)
                return;

            _scheduleRepo.MarkCompleted(_lastDispatchedScheduleId);
            Log.Info($"[PTL-Raw] Completed schedule id={_lastDispatchedScheduleId}");
            _lastDispatchedScheduleId = -1;

            // Pulse ACK so PLC can clear its Complete flag
            PulseAckIfEnabled();
        }

        private void PulseAckIfEnabled()
        {
            if (!_useAck) return;

            // Quick TRUE → FALSE pulse
            TryWriteBool(_ackTagPath, true);
            TryWriteBool(_ackTagPath, false);
        }

        // ================= BIN RESOLUTION =================

        private List<int> ResolveBins(List<string> tokens)
        {
            if (tokens.Count == 0) return new List<int>();

            // Pure numeric → already bins
            if (tokens.All(t => int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                return tokens
                    .Select(t => int.Parse(t, NumberStyles.Integer, CultureInfo.InvariantCulture))
                    .Where(b => b >= 0)
                    .ToList();
            }

            // Otherwise treat as part numbers and map to bin positions
            var binsFromParts = _binRepo.GetBinPositionsForParts(tokens);
            return (binsFromParts ?? new List<int>())
                    .Where(b => b >= 0)
                    .Distinct()
                    .ToList();
        }

        private static List<string> SplitCsv(string csv)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(csv)) return result;

            foreach (var raw in csv.Split(','))
            {
                var t = raw?.Trim();
                if (!string.IsNullOrEmpty(t))
                    result.Add(t);
            }
            return result;
        }

        // ================= Direct PLC I/O =================

        private IUANode GetPlcNodeOrNull()
        {
            var node = Project.Current.Get(_plcNodePath);
            if (node == null)
                Log.Error($"[PTL-Raw] PLC node not found at '{_plcNodePath}'");
            return node;
        }

        private bool TryWriteString(string relativePath, string value)
        {
            try
            {
                var plcNode = GetPlcNodeOrNull();
                if (plcNode == null) return false;

                plcNode.ChildrenRemoteWrite(new List<RemoteChildVariableValue>
                {
                    new RemoteChildVariableValue(relativePath, new UAValue(value ?? string.Empty))
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[PTL-Raw] WriteString failed: {relativePath} → {ex.Message}");
                return false;
            }
        }

        private bool TryWriteBool(string relativePath, bool value)
        {
            try
            {
                var plcNode = GetPlcNodeOrNull();
                if (plcNode == null) return false;

                plcNode.ChildrenRemoteWrite(new List<RemoteChildVariableValue>
                {
                    new RemoteChildVariableValue(relativePath, new UAValue(value))
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[PTL-Raw] WriteBool failed: {relativePath} → {ex.Message}");
                return false;
            }
        }

        private bool? ReadBool(string relativePath)
        {
            try
            {
                var plcNode = GetPlcNodeOrNull();
                if (plcNode == null) return null;

                var reads = plcNode.ChildrenRemoteRead(new List<RemoteChildVariable>
                {
                    new RemoteChildVariable(relativePath)
                });

                // RemoteChildVariableValue is a struct → no null comparison
                foreach (var item in reads)
                {
                    var raw = (item.Value is UAValue ua) ? ua.Value : item.Value;
                    return raw != null ? Convert.ToBoolean(raw) : (bool?)null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[PTL-Raw] ReadBool failed: {relativePath} → {ex.Message}");
                return null;
            }
        }
    }
}
