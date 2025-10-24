using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using System;
using NETCode.Core;
using NETCode.Services;
using NETCode.Entities;
using NETCode.Repositories; // <- make sure this namespace contains PickToLightBinRepository
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

public class CreatePickToLightBinPopupNETLogic : BaseNetLogic
{
    private OptixDBService store;
    private MessageBoxService messageBox;

    // If your OptixDBService doesn't expose the repo, fall back to local instance
    private PickToLightBinRepository Repo =>
        (store?.PickToLightBinRepo as PickToLightBinRepository) ?? new PickToLightBinRepository();

    public override void Start()
    {
        store = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    // ---------------------------
    // INSERT
    // ---------------------------
    [ExportMethod]
    public void InsertPickToLightBin(string binPositionText, string binLabel, string partNumber, string enabledText)
    {
        try
        {
            // 1️ Validate Bin Position is numeric
            if (string.IsNullOrWhiteSpace(binPositionText))
            {
                messageBox.Show("Error", "Bin Position cannot be empty.");
                return;
            }

            if (!int.TryParse(binPositionText.Trim(), out int binPosition))
            {
                messageBox.Show("Error", $"Bin Position '{binPositionText}' is not a valid number. Please enter a numeric value (e.g., 0, 1, 2...).");
                return;
            }

            // Optional: range check (adjust as needed for your rack size)
            if (binPosition < 0 || binPosition > 31)
            {
                messageBox.Show("Error", $"Bin Position {binPosition} is out of valid range (0–31).");
                return;
            }

            // 2️ Validate Enabled flag
            if (!TryParseEnabled(enabledText, out bool active))
            {
                messageBox.Show("Error", "Enabled must be 'Yes' or 'No'.");
                return;
            }

            // 3️ Normalize text fields
            binLabel = NormalizeLabel(binLabel);
            partNumber = NormalizePartNumber(partNumber);

            // 4️ Optional check: bin already in use
            var existing = Repo.GetByBinPosition(binPosition, onlyActive: true);
            if (existing != null && active)
            {
                messageBox.Show("Error", $"Bin position {binPosition} is already active and assigned to part '{existing.PartNumber}'. Please choose another position or deactivate the existing bin first.");
                return;
            }

            // 5️ Create and insert record
            var entity = new PickToLightBin
            {
                BinPosition = binPosition,
                BinLabel = binLabel,
                PartNumber = partNumber,
                Active = active
            };

            Repo.Insert(entity);
            messageBox.Show("Info", $"✅ Bin {binPosition} added successfully. Part: {partNumber}, Active: {(active ? "Yes" : "No")}.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    // ---------------------------
    // UPDATE
    // ---------------------------
    [ExportMethod]
    public void UpdatePickToLightBin(string idText, string binPositionText, string binLabel, string partNumber, string enabledText)
    {
        try
        {
            if (!int.TryParse(idText, out int id) || id <= 0)
            {
                messageBox.Show("Error", "Id is invalid.");
                return;
            }

            if (!TryParseBinPosition(binPositionText, out int binPosition))
            {
                messageBox.Show("Error", "Bin Position must be an integer (e.g., 0..31).");
                return;
            }

            if (!TryParseEnabled(enabledText, out bool active))
            {
                messageBox.Show("Error", "Enabled must be 'Yes' or 'No'.");
                return;
            }

            binLabel = NormalizeLabel(binLabel);
            partNumber = NormalizePartNumber(partNumber);

            // Check existence
            if (!Repo.ExistsById(id))
            {
                messageBox.Show("Error", $"Bin Id {id} not found.");
                return;
            }

            // Guard: if enabling + changing position, ensure no duplicate active bin_position
            var otherActiveSamePos = Repo.GetByBinPosition(binPosition, onlyActive: true);
            if (otherActiveSamePos != null && otherActiveSamePos.Id != id && active)
            {
                messageBox.Show("Error", $"Bin position {binPosition} is already active and mapped to Part '{otherActiveSamePos.PartNumber}'.");
                return;
            }

            var entity = new PickToLightBin
            {
                Id = id,
                BinPosition = binPosition,
                BinLabel = binLabel,
                PartNumber = partNumber,
                Active = active
            };

            Repo.Update(entity);
            messageBox.Show("Info", $"Bin {id} updated. Position: {binPosition}, Part: {partNumber}, Active: {(active ? "Yes" : "No")}.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    // ---------------------------
    // DELETE
    // ---------------------------
    [ExportMethod]
    public void DeletePickToLightBin(string idText)
    {
        try
        {
            if (!int.TryParse(idText, out int id) || id <= 0)
            {
                messageBox.Show("Error", "Id is invalid.");
                return;
            }

            // Assuming your OptixRepositoryBase provides DeleteByID
            Repo.DeleteByID(id);
            messageBox.Show("Info", $"Bin with Id {id} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    // ---------------------------
    // Helpers
    // ---------------------------

    private bool TryParseBinPosition(string input, out int binPosition)
    {
        // Adjust range if your rack has more/less than 32
        if (int.TryParse(input, out binPosition) && binPosition >= 0 && binPosition <= 31)
            return true;

        binPosition = -1;
        return false;
    }

    private bool TryParseEnabled(string enabledText, out bool enabled)
    {
        enabled = false;
        if (string.IsNullOrWhiteSpace(enabledText)) return false;

        if (enabledText.Equals("Yes", StringComparison.OrdinalIgnoreCase)) { enabled = true; return true; }
        if (enabledText.Equals("No", StringComparison.OrdinalIgnoreCase)) { enabled = false; return true; }
        return false;
    }

    private string NormalizeLabel(string label)
    {
        // Keep nulls as null; trim; limit length if you need
        var s = label?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private string NormalizePartNumber(string part)
    {
        // Trim + upper-case to avoid duplicates by case (e.g., ax32 vs AX32)
        var s = part?.Trim();
        return string.IsNullOrEmpty(s) ? "" : s.ToUpperInvariant();
    }
}
