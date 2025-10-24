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

public class EditStationPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void UpdateStation(string name, string description, string dependency, string plcTagName)
    {
        try
        {
            var station = new Station()
            {
                Id = (int)myStore.StationRepo.GetIdByName(name),
                Name = name,
                Description = description,
                Dependency = (int)myStore.StationRepo.GetIdByName(dependency),
                PLCTagName = plcTagName
            };

            myStore.StationRepo.Update(station);

            messageBox.Show("Info", $"Station  {name} - {description} updated successfully.");
        }
        catch (System.Exception ex)
        {
            Log.Error($"Error updating station: {ex.Message}");
        }
    }
}
