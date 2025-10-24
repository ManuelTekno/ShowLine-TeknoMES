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

public class CreateVariantPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;
    private TextBox nameTextBox;
    private TextBox serialCodeTextBox;
    private TextBox descriptionTextBox;
    private IUANode parentContainer;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);

        // Find the container where UI controls are located
        parentContainer = Owner.Owner?.Owner;
        if (parentContainer == null)
        {
            messageBox.Show("Error", "System error: Could not find UI container.");
            return;
        }

        // Get references to UI elements
        nameTextBox = parentContainer.FindObject("txtboxVariantName") as TextBox;
        serialCodeTextBox = parentContainer.FindObject("txtboxSerialCode") as TextBox;
        descriptionTextBox = parentContainer.FindObject("txtboxDescription") as TextBox;
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void CreateNewVariant()
    {
        try
        {
            if (nameTextBox == null || serialCodeTextBox == null || descriptionTextBox == null)
            {
                messageBox.Show("Error", "System error: One or more UI elements are missing.");
                return;
            }

            string name = nameTextBox.Text.Trim();
            string serialCode = serialCodeTextBox.Text.Trim();
            string description = descriptionTextBox.Text.Trim();

            // Validate required fields
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(serialCode))
            {
                messageBox.Show("Warning", "Name and serial code are required.");
                return;
            }

            // Check if Name or serial code already exist
            var existingByName = myStore.VariantsRepo.GetIdByName(name);
            if (existingByName.HasValue)
            {
                messageBox.Show("Warning", "A variant with this Name already exists.");
                return;
            }

            var existingBySerial = myStore.VariantsRepo.GetIdBySerialCode(serialCode);
            if (existingBySerial.HasValue)
            {
                messageBox.Show("Warning", "A variant with this serial code already exists.");
                return;
            }

            // Create new variant
            var newVariant = new Variants
            {
                Name = name,
                SerialCode = serialCode,
                Description = description
            };

            myStore.VariantsRepo.Insert(newVariant);

            // Clear input fields after saving
            nameTextBox.Text = "";
            serialCodeTextBox.Text = "";
            descriptionTextBox.Text = "";

            messageBox.Show("Info", "Variant created successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("CreateVariantLogic", $"Error creating variant: {ex.Message}");
            messageBox.Show("Error", $"Unexpected error: {ex.Message}");
        }
    }
}
