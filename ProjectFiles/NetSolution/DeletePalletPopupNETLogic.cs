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

public class DeletePalletPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void DeletePallet(int id)
    {
        try
        {
            if (id <= 0)
            {
                messageBox.Show("Error", "Invalid Pallet ID.");
                return;
            }

            bool exists = myStore.PalletRepo.ExistsById(id);
            if (!exists)
            {
                messageBox.Show("Error", $"No pallet found with ID {id}.");
                return;
            }

            myStore.PalletRepo.DeleteByID(id);

            messageBox.Show("Info", $"Pallet with ID {id} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", $"Failed to delete pallet: {ex.Message}");
        }
    }
}
