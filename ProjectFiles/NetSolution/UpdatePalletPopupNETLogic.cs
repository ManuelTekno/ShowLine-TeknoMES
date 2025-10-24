using FTOptix.NetLogic;
using NETCode.Entities;
using FTOptix.UI;
using System;
using UAManagedCore;
using NETCode.Core;
using NETCode.Services;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

public class UpdatePalletPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void UpdatePallet(int id, string rfidTag, string description)
    {
        try
        {
            if (id <= 0)
            {
                messageBox.Show("Error", "Invalid Pallet ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rfidTag))
            {
                messageBox.Show("Error", "RFID Tag cannot be blank.");
                return;
            }

            var existingId = myStore.PalletRepo.GetIdByRFIDTag(rfidTag);
            if (existingId.HasValue && existingId.Value != id)
            {
                messageBox.Show("Error", $"Another pallet with RFID tag '{rfidTag}' already exists.");
                return;
            }

            var palletToUpdate = new Pallet
            {
                Id = id,
                RfidTag = rfidTag,
                Description = description
            };

            myStore.PalletRepo.Update(palletToUpdate);
            messageBox.Show("Info", $"Pallet with ID {id} updated successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", $"Failed to update pallet: {ex.Message}");
        }
    }
}
