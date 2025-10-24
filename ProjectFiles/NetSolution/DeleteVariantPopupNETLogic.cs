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

public class DeleteVariantPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void DeleteVariant(string variantId)
    {
        try
        {
            var id = Convert.ToInt32(variantId);

            // Optional: check if variant is in use in another table before deleting
            // For example: bool isUsed = myStore.SomeRepo.ExistsByVariantId(id);

            // Delete variant
            myStore.VariantsRepo.DeleteByID(id);

            messageBox.Show("Info", $"Variant with Id {id} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", $"An error occurred: {ex.Message}");
        }
    }
}
