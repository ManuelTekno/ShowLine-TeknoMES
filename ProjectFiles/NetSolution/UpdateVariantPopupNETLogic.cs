#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using NETCode.Core;
using NETCode.Services;
using NETCode.Entities;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class UpdateVariantPopupNETLogic : BaseNetLogic
{
    private OptixDBService dbService;
    private MessageBoxService messageBox;

    public override void Start()
    {
        dbService = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    public override void Stop()
    {
        // Optional: handle cleanup if needed
    }

    [ExportMethod]
    public void UpdateSelectedVariant(string selectedVariantName, string serialCode, string description)
    {
        try
        {

            // Validate required fields
            if (string.IsNullOrWhiteSpace(selectedVariantName) || string.IsNullOrWhiteSpace(serialCode))
            {
                messageBox.Show("Warning", "Variant name and serial code are required.");
                return;
            }

            selectedVariantName = selectedVariantName.Trim();
            serialCode = serialCode.Trim();
            description = description?.Trim();

            // Get ID of the selected variant
            var variantId = dbService.VariantsRepo.GetIdByName(selectedVariantName);
            if (!variantId.HasValue)
            {
                messageBox.Show("Error", "The selected variant does not exist.");
                return;
            }

            // Check for serial code duplication
            var existingBySerial = dbService.VariantsRepo.GetIdBySerialCode(serialCode);
            if (existingBySerial.HasValue && existingBySerial.Value != variantId.Value)
            {
                messageBox.Show("Warning", "Another variant with this serial code already exists.");
                return;
            }

            // Update the variant
            var updatedVariant = new Variants
            {
                Id = variantId.Value,
                Name = selectedVariantName,
                SerialCode = serialCode,
                Description = description
            };

            dbService.VariantsRepo.Update(updatedVariant);

            messageBox.Show("Info", "Variant updated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("UpdateVariantLogic", $"Error updating variant: {ex.Message}");
            messageBox?.Show("Error", $"Unexpected error: {ex.Message}");
        }
    }
}
