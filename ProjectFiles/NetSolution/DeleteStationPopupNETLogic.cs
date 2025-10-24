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

public class DeleteStationPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void DeleteStation(int stationId)
    {
        try
        {
            myStore.StationRepo.DeleteByID(stationId);

            messageBox.Show("Info", $"Station with Id {stationId} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

}
