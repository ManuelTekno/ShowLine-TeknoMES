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

public class CreateStationPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void InsertStation(string desc, string name, int dependency, string plcTagName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                messageBox.Show("Error", "Station Name field cannot be blank.");
                return;
            }

            var stationExists = myStore.StationRepo.GetIdByName(name);
            if (stationExists > 0)
            {
                messageBox.Show("Error", $"{name} already exists, try another Name.");
                return;
            }

            var stationEntity = new Station
            {
                Dependency = dependency > 0 ? dependency : 0,
                Description = desc,
                Name = name,
                PLCTagName = plcTagName
            };

            myStore.StationRepo.Insert(stationEntity);

            messageBox.Show("Info", $"{name} added successfully to Stations table.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    [ExportMethod]
    public void DeleteStation(string stationId)
    {
        try
        {
            var idStation = Convert.ToInt32(stationId);
            bool hasOperations = myStore.OperationRepo.ExistsByRecipeId(idStation);

            if (hasOperations)
            {
                //Delete related operations
                myStore.OperationRepo.DeleteByRecipeId(idStation);
            }

            //Delete station
            myStore.StationRepo.DeleteByID(idStation);

            messageBox.Show("Info", $"Station with Id {idStation} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    [ExportMethod]
    public void UpdateStation(string name, string description, string dependency , string plcTagName)
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

            messageBox.Show("Info", $"Station {name} - {description} updated successfully.");
        }
        catch (System.Exception ex)
        {
            Log.Error($"Error updating station: {ex.Message}");
        }
    }
}
