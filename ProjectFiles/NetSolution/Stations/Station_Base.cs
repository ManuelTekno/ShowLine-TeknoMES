using System;
using System.Threading.Tasks;
using UAManagedCore;
using NETCode.Services;
using NETCode.Core;
using NETCode.Entities;
using System.Collections.Generic;
using System.Globalization;
using NETCode.Stations.Operations;
using System.Threading;
using FTOptix.HMIProject;
using System.Collections.Immutable;

namespace NETCode.Stations;
public class Station_Base : IStation
{
    public readonly string _stationTag;
    private readonly IPLCTagService _plc;
    private readonly OptixDBService _db;
    private readonly string _stationNameInDB;
    private List<Operation> _loadedOperations = new();
    private List<Operation> _failedOperations = new();
    private Dictionary<int, int> _operationRetries = new();
    private bool _unitFailed = false;
    private int countdownRemaining = -1;
    private readonly int _stationId;                          
    private static volatile List<StationRoute> _routeCache = new();
    private static DateTime _routeCacheLoadedAt = DateTime.MinValue;
    private static readonly TimeSpan _routeCacheTtl = TimeSpan.FromSeconds(60);
    private bool _opsFinished = false;



    public Station_Base(string stationTag, string stationNameInDB, IPLCTagService plcTagService)
    {
        _stationTag = stationTag;
        _plc = plcTagService;
        _db = StationServiceManager.Services[stationTag].DBService;
        _stationNameInDB = stationNameInDB;

        var id = _db.StationRepo.GetIdByName(_stationNameInDB);
        if (!id.HasValue)
            throw new InvalidOperationException($"Station '{_stationNameInDB}' not found in 'stations' table.");
        _stationId = id.Value;
    }

    public void RunCycle()
    {

        int? commandRaw = _plc.ReadIntTag("To/Command");
        if (!commandRaw.HasValue || commandRaw.Value == 0)
            return;

        byte commandValue;
        try { commandValue = Convert.ToByte(commandRaw.Value); }
        catch
        {
            Log.Error($"[{_stationTag}] Invalid Command value: {commandRaw.Value}");
            _plc.WriteSingleTag("To/Command", 0);
            return;
        }

        HandleCommand(commandValue);
        _plc.WriteSingleTag("To/Command", 0);
    }


    private void HandleCommand(int commandValue)
    {
        Log.Info($"[{_stationTag}] Command received: {commandValue}");

        switch ((StationCommand)commandValue)
        {
            case StationCommand.Initialize:
                Initialize(); break;
            case StationCommand.RegisterUnit:
                RegisterUnit(); break;
            case StationCommand.CheckUnit:
                CheckUnit(); break;
            case StationCommand.LoadOperations:
                LoadOperations(); break;
            case StationCommand.ExecuteOperations:
                ExecuteOperations(); break;
            case StationCommand.SaveUnitResults:
                SaveUnitResults(); break;
            case StationCommand.ArchiveUnit:
                ArchiveUnit(); break;
            case StationCommand.MarkUnitAsRework:
                MarkUnitAsRework(); break;
            default:
                Log.Warning($"[{_stationTag}] Unknown command: {commandValue}");
                break;
        }
    }

    public void Initialize()
    {
        Log.Info($"[{_stationTag}] Initializing...");

        _plc.ClearAllTags();
        _unitFailed = false;
        _failedOperations?.Clear();
        _operationRetries?.Clear();
        countdownRemaining = -1;
        _opsFinished = false;


    Log.Info($"[{_stationTag}] ✅ Unit Initialized");
        _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
    }

    public void RegisterUnit()
    {
        try
        {
            var palletCode = _plc.ReadStringTag("To/PalletID");
            var validationCode = _plc.ReadStringTag("To/ValidationCode");

            if (string.IsNullOrEmpty(palletCode) || string.IsNullOrEmpty(validationCode))
            {
                Log.Error($"[{_stationNameInDB}] Missing PalletCode or ValidationCode");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Pallet '{palletCode}' not found in DB.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }

            var variant = _db.VariantsRepo.FindByPartialSerialMatch(validationCode);
            if (variant == null)
            {
                Log.Error($"[{_stationNameInDB}] No variant found for code '{validationCode}'.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.VariantNotFound);
                return;
            }

            // --- CHEQUEO DE SERIAL REPETIDO (usando tus repos existentes) ---
            var existingUnit = _db.ProductionUnitRepo.FindBySerialCode(validationCode);
            if (existingUnit != null)
            {
                if (existingUnit.IsArchived == false)
                {
                    // Ya hay una activa con ese SerialCode → 903
                    Log.Warning($"[{_stationNameInDB}] Unit with Serial='{validationCode}' is already active.");
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitAlreadyExists); // 903
                    return;
                }

                // Existe pero archivada → REACTIVAR LA MISMA FILA (UPDATE)
                var stationId = _db.StationRepo.GetIdByName(_stationNameInDB) ?? 0;

                existingUnit.CurrentStationId = stationId;
                existingUnit.PalletId = palletId.Value;
                existingUnit.UnitStatus = "In_Process";
                existingUnit.QualityStatus = "Pending";
                existingUnit.IsArchived = false;
                existingUnit.FinishedAt = null;
                // (Opcional) decide si actualizas CreationDate o lo conservas
                // existingUnit.CreationDate  = DateTime.Now;

                _db.ProductionUnitRepo.Update(existingUnit);

                Log.Info($"[{_stationNameInDB}] ♻️ Unit reactivated from archive: {validationCode}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                return;
            }

            // --- NO EXISTE → INSERT NORMAL ---
            var newStationId = _db.StationRepo.GetIdByName(_stationNameInDB) ?? 0;

            _db.ProductionUnitRepo.Insert(new ProductionUnit
            {
                SerialCode = validationCode,
                CreationDate = DateTime.Now,
                UnitStatus = "In_Process",
                QualityStatus = "Pending",
                CurrentStationId = newStationId,
                PalletId = palletId.Value,
                VariantId = variant.Id,
                IsArchived = false,
                FinishedAt = null
            });

            Log.Info($"[{_stationNameInDB}] ✅ Unit successfully registered: {validationCode}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Unexpected error while registering unit: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    public void CheckUnit()
    {
        try
        {
            var palletCode = _plc.ReadStringTag("To/PalletID");
            var validationCode = _plc.ReadStringTag("To/ValidationCode");

            if (string.IsNullOrEmpty(palletCode))
            {
                Log.Error($"[{_stationNameInDB}] Missing PalletCode.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Pallet '{palletCode}' not found in DB.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }

            if (string.IsNullOrEmpty(validationCode))
            {
                validationCode = GetValidationCodeFromDB(palletId.Value);
                if (string.IsNullOrEmpty(validationCode))
                {
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                    return;
                }
            }
            _plc.WriteSingleTag("To/ValidationCode", validationCode);

            var unit = _db.ProductionUnitRepo.FindBySerialAndPallet(validationCode, palletId.Value);
            if (unit == null)
            {
                Log.Error($"[{_stationNameInDB}] No unit found with Serial='{validationCode}' and PalletID={palletId.Value}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                return;
            }

            // Determine if this station is terminal (no children depend on it)
            bool isTerminal = _db.StationRepo.IsTerminalById(_stationId);

            // EARLY EXIT IF REWORK (before station/dependency checks) — except at terminal
            if (unit.QualityStatus == "Rework" && !isTerminal)
            {
                var dest = ResolveDestinationById(_stationId, "Rework");
                _plc.WriteSingleTag("From/Destination", dest);
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Rework);
                Log.Warning($"[{_stationNameInDB}] Unit is REWORK. Destination set to {dest}.");
                return;
            }
            else if (unit.QualityStatus == "Rework" && isTerminal)
            {
                // Terminal station: ignore rework and continue
                Log.Warning($"[{_stationNameInDB}] Unit is REWORK, but station is TERMINAL. Ignoring rework and proceeding.");
                // Do NOT set destination/response here — continue with normal checks
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                return;

            }

            // Normal path: verify it is allowed to be processed here
            var expectedDependencyId = _db.StationRepo.GetDependencyByName(_stationNameInDB);
            if (expectedDependencyId == null)
            {
                Log.Error($"[{_stationNameInDB}] No dependency ID found for station '{_stationNameInDB}'");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
                return;
            }

            if (unit.CurrentStationId != expectedDependencyId.Value)
            {
                Log.Warning($"[{_stationNameInDB}] Unit is not assigned to this station. CurrentStation={unit.CurrentStationId}, Expected Dependency={expectedDependencyId}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotAtThisStation);
                return;
            }

            Log.Info($"[{_stationNameInDB}] ✅ Unit verified successfully.");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Unexpected error in CheckUnit: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    public void LoadOperations()
    {
        try
        {
            Log.Info($"[{_stationNameInDB}] Loading operations...");
            var palletCode = _plc.ReadStringTag("To/PalletID");
            if (string.IsNullOrEmpty(palletCode))
            {
                Log.Error($"[{_stationNameInDB}] Missing PalletCode.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Pallet '{palletCode}' not found in DB.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }
            var validationCode = _plc.ReadStringTag("To/ValidationCode");
            if (string.IsNullOrEmpty(validationCode))
            {
                validationCode = GetValidationCodeFromDB(palletId.Value);
                if (string.IsNullOrEmpty(validationCode))
                {
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                    return;
                }
                else Log.Info(validationCode);

            }
            var variant = _db.VariantsRepo.FindByPartialSerialMatch(validationCode);
            if (variant == null)
            {
                Log.Error($"[{_stationNameInDB}] No variant found for code '{validationCode}'.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.VariantNotFound);
                return;
            }
            Log.Info(variant.Name);


            var stationId = _db.StationRepo.GetIdByName(_stationNameInDB);
            if (stationId == null)
            {
                Log.Error($"[{_stationNameInDB}] Station '{_stationNameInDB}' not found.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
                return;
            }
            Log.Info(stationId.ToString()); ;

            var recipe = _db.RecipeRepo.FindByVariantAndStation(variant.Id, stationId.Value);
            if (recipe == null)
            {
                Log.Info($"[{_stationNameInDB}] No recipe found for Variant = {variant.Name}");
                //_plc.WriteSingleTag("From/Response", CommandResponseCodes.RecipeNotFound);
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                return;
            }
            Log.Info(recipe.Name);


            var operations = _db.OperationRepo.GetByRecipeId(recipe.ID);
            if (operations == null || operations.Count == 0)
            {
                Log.Warning($"[{_stationNameInDB}] No operations found for RecipeID={recipe.ID}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                return;
            }
            WriteOperationsToPLC(operations);
            _loadedOperations = operations;


            Log.Info($"[{_stationNameInDB}] ✅ {operations.Count} operations written to PLC.");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Error in LoadOperations: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }

        return;
    }

    public void ExecuteOperations()
    {
        if (_opsFinished)
        {
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
            return;
        }

        if (_loadedOperations == null || _loadedOperations.Count == 0)
        {
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
            return;
        }

        int? opIndex = _plc.ReadIntTag("From/Operation_Index");
        if (opIndex == null) { _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError); return; }

        var currentOp = _loadedOperations.Find(op => op.Index == opIndex.Value);
        if (currentOp == null) { _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError); return; }

        try
        {
            var behavior = BehaviorFactory.GetBehavior(currentOp.BehaviorID);
            var outcome = behavior.Execute(this, currentOp);

            switch (outcome)
            {
                case OperationResult.Waiting:
                    return;

                case OperationResult.Passed:
                    if (_operationRetries.ContainsKey(currentOp.ID))
                        _operationRetries.Remove(currentOp.ID);

                    int nextIndex = currentOp.Index + 1;
                    WriteSingleTag("From/Operation_Index", nextIndex);

                    int lastIndex = _loadedOperations[_loadedOperations.Count - 1].Index;
                    if (nextIndex > lastIndex)
                    {
                        _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                    }
                    return;

                case OperationResult.Failed:
                    if (!_operationRetries.ContainsKey(currentOp.ID))
                        _operationRetries[currentOp.ID] = 1;
                    else
                        _operationRetries[currentOp.ID]++;

                    Log.Error($"[{_stationNameInDB}] Op '{currentOp.Description}' FAILED. Retry #{_operationRetries[currentOp.ID]}.");

                    const int MAX_RETRIES = 3;
                    if (_operationRetries[currentOp.ID] > MAX_RETRIES)
                    {
                        // 1) Mark operation as failed (DO NOT write From/Response here)
                        MarkOperationFailed(currentOp);

                        // 2) Ensure result tags reflect a failed/neutral state for this op
                        string toComplete = $"To/Results/{currentOp.Index}/Complete";
                        string toStatus = $"To/Results/{currentOp.Index}/Status";
                        _plc.WriteSingleTag(toComplete, false);
                        _plc.WriteSingleTag(toStatus, 0);

                        // 3) Move to the next operation (do not jump to the end)
                         nextIndex = currentOp.Index + 1;
                        WriteSingleTag("From/Operation_Index", nextIndex);

                        // 4) If there are no more operations, now close with Success
                        lastIndex = _loadedOperations[_loadedOperations.Count - 1].Index;
                        if (nextIndex > lastIndex)
                        {
                            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
                            _opsFinished = true;
                        }
                        return;
                    }

                    // Still have retries → let PLC know op failed, keep command alive
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.OperationFailed);
                    return;


            }
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Op '{currentOp?.Description}' error: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    public void SaveUnitResults()
    {
        try
        {
            var palletCode = _plc.ReadStringTag("To/PalletID");
            var validationCode = _plc.ReadStringTag("To/ValidationCode");

            if (string.IsNullOrEmpty(palletCode))
            {
                Log.Error($"[{_stationNameInDB}] Missing PalletCode.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Pallet '{palletCode}' not found in DB.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }

            if (string.IsNullOrEmpty(validationCode))
            {
                validationCode = GetValidationCodeFromDB(palletId.Value);
                if (string.IsNullOrEmpty(validationCode))
                {
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                    return;
                }
            }
            var currentCycleTime = _plc.ReadFloatTag("To/CycleTime");

            var unit = _db.ProductionUnitRepo.FindBySerialAndPallet(validationCode, palletId.Value);
            if (unit == null)
            {
                Log.Error($"[{_stationNameInDB}] Unit not found with Serial='{validationCode}' and PalletID={palletId.Value}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                return;
            }

            var stationId = _db.StationRepo.GetIdByName(_stationNameInDB);
            if (stationId == null)
            {
                Log.Error($"[{_stationNameInDB}] Station ID not found for name '{_stationNameInDB}'");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
                return;
            }

            string finalStatus = _unitFailed ? "Failed" : "Completed";
            string resultStatus = _unitFailed ? "Failed" : "Completed";

            var stationResult = new ProductionUnitResult
            {
                UnitId = unit.Id,
                StationId = stationId.Value,
                CycleTime = currentCycleTime,
                Status = resultStatus,
                FinishedAt = DateTime.Now
            };
            _db.ProductionUnitStationResultRepo.Insert(stationResult);

            if (_loadedOperations == null || _loadedOperations.Count == 0)
                goto SkipOperationLoop;

            foreach (var op in _loadedOperations)
            {
                var (parameterConfigured, paramValueRead) = ReadOperationResult(op);

                var operationResult = new ProductionUnitOperationResult
                {
                    UnitId = unit.Id,
                    OperationId = op.ID,
                    Name = BuildParameterName(op) ?? "Unknown_Parameter",
                    Parameter = parameterConfigured,
                    Value = paramValueRead,
                    Result = _failedOperations.Contains(op) ? "Fail" : "Pass"
                };

                if (!string.IsNullOrWhiteSpace(operationResult.Name) && operationResult.Value != null)
                {
                    _db.ProductionUnitOperationResultRepo.Insert(operationResult);
                }
            }

        SkipOperationLoop:

            string unitStatusToSet = "In_Process";                     
            string qualityStatusToSet = _unitFailed ? "Rework" : "Pass"; 
            _db.ProductionUnitRepo.UpdateCurrentStation(
                validationCode,
                palletId.Value,
                stationId.Value,
                unitStatusToSet,
                qualityStatusToSet
            );

            if (_unitFailed)
            {
                var dest = ResolveDestinationById(_stationId, "Rework");
                _plc.WriteSingleTag("From/Destination", dest);
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Rework);
            }
            else
            {
                var dest = ResolveDestinationById(_stationId, "Pass");
                _plc.WriteSingleTag("From/Destination", dest);
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
            }
        }

        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Error in SaveUnitResults: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    public void ArchiveUnit()
    {
        try
        {
            var palletCode = _plc.ReadStringTag("To/PalletID");
            var validationCode = _plc.ReadStringTag("To/ValidationCode");

            if (string.IsNullOrEmpty(palletCode))
            {
                Log.Error($"[{_stationNameInDB}] Missing PalletCode.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Pallet '{palletCode}' not found in DB.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }

            if (string.IsNullOrEmpty(validationCode))
            {
                validationCode = GetValidationCodeFromDB(palletId.Value);
                if (string.IsNullOrEmpty(validationCode))
                {
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                    return;
                }
            }
            var unit = _db.ProductionUnitRepo.FindBySerialAndPallet(validationCode, palletId.Value);
            if (unit == null)
            {
                Log.Error($"[{_stationNameInDB}] Unit not found with Serial='{validationCode}' and PalletID={palletId.Value}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                return;
            }
            _db.ProductionUnitRepo.ArchiveProductionUnit(unit.Id);
            Log.Info($"[{_stationNameInDB}] ✅ Unit archived successfully.");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Error in ArchiveUnit: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    private void MarkUnitAsRework()
    {
        _unitFailed = true;

        try
        {
            var palletCode = _plc.ReadStringTag("To/PalletID");
            var validationCode = _plc.ReadStringTag("To/ValidationCode");

            if (string.IsNullOrEmpty(palletCode))
            {
                Log.Error($"[{_stationNameInDB}] Cannot mark as REWORK: missing PalletID.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.InvalidIdOrValidationCode);
                return;
            }

            var palletId = _db.PalletRepo.GetIdByRFIDTag(palletCode);
            if (palletId == null)
            {
                Log.Error($"[{_stationNameInDB}] Cannot mark as REWORK: Pallet '{palletCode}' not found.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.PalletNotFound);
                return;
            }

            if (string.IsNullOrEmpty(validationCode))
            {
                validationCode = GetValidationCodeFromDB(palletId.Value);
                if (string.IsNullOrEmpty(validationCode))
                {
                    Log.Error($"[{_stationNameInDB}] Cannot mark as REWORK: unit not found for PalletID={palletId.Value}.");
                    _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                    return;
                }
                _plc.WriteSingleTag("To/ValidationCode", validationCode);
            }

            // Load unit for traceability (Id, current status)
            var unit = _db.ProductionUnitRepo.FindBySerialAndPallet(validationCode, palletId.Value);
            if (unit == null)
            {
                Log.Error($"[{_stationNameInDB}] Unit not found with Serial='{validationCode}' and PalletID={palletId.Value}");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.UnitNotFound);
                return;
            }

            // Resolve current station ID strictly (no fallback to previous station)
            var stationIdOpt = _db.StationRepo.GetIdByName(_stationNameInDB);
            if (!stationIdOpt.HasValue)
            {
                Log.Error($"[{_stationNameInDB}] Station not found by name '{_stationNameInDB}'.");
                _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
                return;
            }
            var stationId = stationIdOpt.Value;

            // Persist current station + quality only (keep UnitStatus as-is or default to In_Process)
            _db.ProductionUnitRepo.UpdateCurrentStation(
                validationCode,
                palletId.Value,
                stationId,
                unit.UnitStatus ?? "In_Process",
                "Rework"
            );

            // (Traceability) Insert a station result entry with "Rework"
            _db.ProductionUnitStationResultRepo.Insert(new ProductionUnitResult
            {
                UnitId = unit.Id,
                StationId = stationId,
                CycleTime = _plc.ReadFloatTag("To/CycleTime"),
                Status = "Rework",
                FinishedAt = DateTime.Now
            });

            // Re-read unit to verify in logs
            var verify = _db.ProductionUnitRepo.FindBySerialAndPallet(validationCode, palletId.Value);
            Log.Info($"[{_stationNameInDB}] After REWORK update → CurrentStationId={verify?.CurrentStationId}, QualityStatus={verify?.QualityStatus}");

            var dest = ResolveDestinationById(_stationId, "Rework");
            _plc.WriteSingleTag("From/Destination", dest);       // SINT
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.Rework);
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationNameInDB}] Error while marking unit as REWORK: {ex.Message}");
            _plc.WriteSingleTag("From/Response", CommandResponseCodes.GeneralError);
        }
    }

    //Helpers for OperationBehavior Class
    public bool ReadBoolTag(string tag) => _plc.ReadBoolTag(tag) ?? false;
    public object ReadGenericTag(string tag) => _plc.ReadSingleTag(tag);
    public string ReadStringTag(string tag) => _plc.ReadStringTag(tag);
    public int? ReadIntTag(string tag) => _plc.ReadIntTag(tag);

    public float? ReadFloatTag(string tag) => _plc.ReadFloatTag(tag);
    public void WriteSingleTag(string tag, object value) => _plc.WriteSingleTag(tag, value);
    public string GetStationTag() => _stationTag;

    //Internal helpers for the class
    private static string BuildParameterName(Operation op)
    {
        string typeName = op.OperationTypeID switch
        {
            1 => "Timed",
            2 => "Robot",
            3 => "TorqueTool",
            4 => "ExternalInput",
            5 => "Barcode",
            6 => "Camera Inspection",
            7 => "Pick To Light",
            _ => "UnknownType"
        };

        string behaviorName = op.BehaviorID switch
        {
            1 => "WaitComplete",
            2 => "ValidateValue",
            3 => "SendCommand",
            4 => "DisplayOnly",
            5 => "UserConfirm",
            6 => "StartTimer",
            7 => "CheckCointainsValue",
            8 => "CaptureResult",
            9 => "SendListWaitComplete",

            _ => "UnknownBehavior"
        };

        return $"{typeName} / {behaviorName} / {op.Description}";
    }
    private void MarkUnitAsRework(Operation op)
    {
        if (!_failedOperations.Contains(op))
            _failedOperations.Add(op);

        _unitFailed = true;

        var operationName = BuildParameterName(op);

        Log.Error($"[{_stationNameInDB}] ❌ Operation {op.Index} - '{operationName}' failed too many times. Marking unit as Rework.");
        _plc.WriteSingleTag("From/Response", CommandResponseCodes.Success);
    }
    private void WriteOperationsToPLC(List<Operation> operations)
    {
        var writeList = new List<RemoteChildVariableValue>();

        for (int i = 0; i < 10; i++)
        {
            string prefix = $"From/Operations/{i}/";
            writeList.Add(new RemoteChildVariableValue(prefix + "Enable", new UAValue(false)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Type", new UAValue((short)0)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Behavior", new UAValue((short)0)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Value_STRING", new UAValue("")));
            writeList.Add(new RemoteChildVariableValue(prefix + "Value_REAL", new UAValue(0.0f)));
        }

        for (int i = 0; i < Math.Min(operations.Count, 10); i++)
        {
            var op = operations[i];
            string prefix = $"From/Operations/{i}/";

            writeList.Add(new RemoteChildVariableValue(prefix + "Enable", new UAValue(true)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Type", new UAValue((short)op.OperationTypeID)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Behavior", new UAValue((short)op.BehaviorID)));
            writeList.Add(new RemoteChildVariableValue(prefix + "Value_STRING", new UAValue(op.ValueString ?? "")));
            writeList.Add(new RemoteChildVariableValue(prefix + "Value_REAL", new UAValue(op.ValueReal ?? 0.0f)));
        }

        _plc.WriteMultipleTags(writeList);
    }
    private (string Parameter, string Value) ReadOperationResult(Operation op)
    {
        string parameterConfigured = "N/A";
        string paramValueRead = "N/A";

        bool isCaptureResult = op.BehaviorID == (int)OperationBehaviorNames.CaptureResult;
        bool isSendCommand = op.BehaviorID == (int)OperationBehaviorNames.SendCommand;
        bool isStartTimer = op.BehaviorID == (int)OperationBehaviorNames.StartTimer;
        bool isDisplayOnly = op.BehaviorID == (int)OperationBehaviorNames.DisplayOnly;
        bool isUserConfirm = op.BehaviorID == (int)OperationBehaviorNames.UserConfirm;

        Log.Info($"[ReadOperationResult] Processing Operation {op.Index} (Behavior: {op.BehaviorID})...");

        if (isSendCommand || isStartTimer)
        {
            if (!string.IsNullOrEmpty(op.ValueString))
            {
                parameterConfigured = op.ValueString;
            }
            else if (op.ValueReal.HasValue)
            {
                parameterConfigured = op.ValueReal.Value.ToString();
            }

            paramValueRead = "N/A";
        }
        else if (isDisplayOnly || isUserConfirm)
        {
            parameterConfigured = "N/A";
            paramValueRead = "N/A";
        }
        else if (isCaptureResult)
        {
            parameterConfigured = "N/A";

            string actualString = _plc.ReadStringTag($"To/Results/{op.Index}/Result_STRING");
            if (!string.IsNullOrWhiteSpace(actualString))
            {
                paramValueRead = actualString;
            }
            else
            {
                float? actualReal = _plc.ReadFloatTag($"To/Results/{op.Index}/Result_REAL");
                if (actualReal.HasValue && Math.Abs(actualReal.Value) > 0.0001)
                {
                    paramValueRead = actualReal.Value.ToString();
                }
                else
                {
                    paramValueRead = "N/A";
                    Log.Warning($"[CaptureResult] No valid STRING or REAL (>0) found for Operation {op.Index}");
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(op.ValueString))
            {
                parameterConfigured = op.ValueString;
                string actualString = _plc.ReadStringTag($"To/Results/{op.Index}/Result_STRING");

                Log.Info($"[ReadOperationResult] Expected STRING: '{parameterConfigured}', Actual STRING: '{actualString}'");

                if (!string.IsNullOrWhiteSpace(actualString))
                    paramValueRead = actualString;
                else
                    paramValueRead = "N/A";
            }
            else if (op.ValueReal.HasValue && Math.Abs(op.ValueReal.Value) > 0.0001)
            {
                parameterConfigured = op.ValueReal.Value.ToString();
                float? actualReal = _plc.ReadFloatTag($"To/Results/{op.Index}/Result_REAL");

                Log.Info($"[ReadOperationResult] Expected REAL: '{parameterConfigured}', Actual REAL: '{actualReal}'");

                if (actualReal.HasValue && Math.Abs(actualReal.Value) > 0.0001)
                    paramValueRead = actualReal.Value.ToString();
                else
                    paramValueRead = "N/A";
            }
            else
            {
                Log.Warning($"[ReadOperationResult] Operation {op.Index} has neither valid STRING nor REAL (>0).");
                parameterConfigured = "N/A";
                paramValueRead = "N/A";
            }
        }

        Log.Info($"[ReadOperationResult] Final PARAMETER: '{parameterConfigured}', VALUE: '{paramValueRead}'");

        return (parameterConfigured, paramValueRead);
    }
    public bool StartCountdown(double seconds)
    {
        if (countdownRemaining < 0)
        {
            countdownRemaining = (int)Math.Ceiling(seconds);
            _plc.WriteSingleTag("From/RemainingTime", countdownRemaining);
            return false;
        }

        if (countdownRemaining > 0)
        {
            countdownRemaining--;
            _plc.WriteSingleTag("From/RemainingTime", countdownRemaining);
            return false;
        }

        // Timer terminado
        _plc.WriteSingleTag("From/RemainingTime", 0);
        countdownRemaining = -1;
        return true;
    }
    private string GetValidationCodeFromDB(int palletId)
    {
        var unit = _db.ProductionUnitRepo.FindByPallet(palletId);
        if (unit == null)
        {
            Log.Error($"[{_stationNameInDB}] No active unit found with PalletID={palletId}");
            return null;
        }
        return unit.SerialCode;
    }
    private void MarkOperationFailed(Operation op)
    {
        if (!_failedOperations.Contains(op))
            _failedOperations.Add(op);

        _unitFailed = true;

        var operationName = BuildParameterName(op);
        Log.Error($"[{_stationNameInDB}] ❌ Operation {op.Index} - '{operationName}' exceeded retries. Marked as FAILED; continuing with next operation.");
    }


    // cache refresh (static; shared across instances)
    private void RefreshRoutesIfStale()
    {
        if ((DateTime.UtcNow - _routeCacheLoadedAt) < _routeCacheTtl && _routeCache.Count > 0)
            return;

        // Pull **enabled-only** rules ordered by priority (lowest wins)
        _routeCache = _db.StationRoutesRepo.GetEnabledRulesOrderedByPriority(); // implement as shown earlier
        _routeCacheLoadedAt = DateTime.UtcNow;
    }

    // returns 1=Forward (default), 2=Left, 3=Right
    private sbyte ResolveDestinationById(int stationId, string quality)
    {
        RefreshRoutesIfStale();

        // try exact quality, then 'Any'
        var q = quality ?? "Any";

        foreach (var tryQ in new[] { q, "Any" })
        {
            var rule = _routeCache.Find(r =>
                r.Enabled &&
                r.StationId == stationId &&
                string.Equals(r.Quality, tryQ, StringComparison.OrdinalIgnoreCase));
            if (rule != null) return rule.Destination;
        }

        return (sbyte)1; // Forward default
    }


}
