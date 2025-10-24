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

public class CreatePalletPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void InsertPallet(string rfidTag, string description)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rfidTag))
            {
                messageBox.Show("Error", "RFID Tag field cannot be blank.");
                return;
            }

            var palletExists = myStore.PalletRepo.GetIdByRFIDTag(rfidTag);
            if (palletExists.HasValue)
            {
                messageBox.Show("Error", $"A pallet with RFID tag '{rfidTag}' already exists.");
                return;
            }

            var palletEntity = new Pallet
            {
                RfidTag = rfidTag,
                Description = description
            };

            myStore.PalletRepo.Insert(palletEntity);

            messageBox.Show("Info", $"Pallet with RFID tag '{rfidTag}' added successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }
}
