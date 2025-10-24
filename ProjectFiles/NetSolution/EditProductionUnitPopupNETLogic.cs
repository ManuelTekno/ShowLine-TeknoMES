using FTOptix.NetLogic;
using FTOptix.UI;
using NETCode.Services;
using UAManagedCore;
using System;
using NETCode.Core;

public class EditProductionUnitPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }


    [ExportMethod]
    public void UpdateProductionUnitByNames(string serialCode, string palletId, string unitStatus, string qualityStatus, string lastStationName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialCode))
                throw new Exception("Serial code is required.");

            var unit = myStore.ProductionUnitRepo.FindBySerialCode(serialCode);
            if (unit == null)
                throw new Exception($"Production unit with Serial '{serialCode}' was not found.");

            int? newPalletId = ParseNullableInt(palletId);
            string newUnitStatus = string.IsNullOrWhiteSpace(unitStatus) ? null : unitStatus.Trim();
            string newQualityStatus = string.IsNullOrWhiteSpace(qualityStatus) ? null : qualityStatus.Trim();

            int? newStationId = null;
            if (!string.IsNullOrWhiteSpace(lastStationName))
            {
                var stationIdObj = myStore.StationRepo.GetIdByName(lastStationName.Trim());
                if (stationIdObj == null)
                    throw new Exception($"Station '{lastStationName}' not found.");
                newStationId = (int)stationIdObj;
            }

            if (newPalletId.HasValue && newPalletId.Value != unit.PalletId)
            {
                bool exists = myStore.ProductionUnitRepo.ExistsActiveByPallet(newPalletId.Value, unit.Id);
                if (exists)
                    throw new Exception($"Pallet ID {newPalletId.Value} is already in use by another active unit (is_archived = 0).");
            }

            myStore.ProductionUnitRepo.UpdatePartial(
                id: unit.Id,
                palletId: newPalletId,
                unitStatus: newUnitStatus,
                qualityStatus: newQualityStatus,
                currentStationId: newStationId
            );


            messageBox.Show("Info", $"Unit '{serialCode}' updated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error($"[UpdateProductionUnitByNames] {ex.Message}");
            messageBox.Show("Error", $"Update failed: {ex.Message}");
        }
    }

    [ExportMethod]
    public void DeleteProductionUnitBySerial(string serialCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialCode))
                throw new Exception("Serial code is required.");

            var unit = myStore.ProductionUnitRepo.FindBySerialCode(serialCode);
            if (unit == null)
                throw new Exception($"Unit with Serial '{serialCode}' was not found.");

            myStore.ProductionUnitRepo.HardDeleteById(unit.Id);
            messageBox.Show("Info", $"Unit '{serialCode}' deleted permanently.");
        }
        catch (Exception ex)
        {
            Log.Error($"[DeleteProductionUnitBySerial] {ex.Message}");
            messageBox.Show("Error", $"Delete failed: {ex.Message}");
        }
    }

    [ExportMethod]
    public void ArchiveProductionUnitBySerial(string serialCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialCode))
                throw new Exception("Serial code is required.");

            myStore.ProductionUnitRepo.ArchiveBySerial(serialCode);
            messageBox.Show("Info", $"Unit '{serialCode}' archived successfully.");
        }
        catch (Exception ex)
        {
            Log.Error($"[ArchiveProductionUnitBySerial] {ex.Message}");
            messageBox.Show("Error", $"Archive failed: {ex.Message}");
        }
    }

    // Helper: parsea int? desde string (vacío/null => null)
    private int? ParseNullableInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!int.TryParse(s.Trim(), out var v))
            throw new Exception($"Value '{s}' must be an integer.");
        return v;
    }
}
