using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using System;
using NETCode.Core;
using NETCode.Services;
using NETCode.Entities;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

public class CreateStationRoutePopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }


    // ---------------------------
    // INSERT
    // ---------------------------
    [ExportMethod]
    public void InsertStationRoute(int stationId, string quality, sbyte destinationId, int priority, string enabledText)
    {
        try
        {
            // IDs básicos
            if (!StationExists(stationId))
            {
                messageBox.Show("Error", $"StationId {stationId} is invalid or not found.");
                return;
            }

            // Quality
            if (!ValidateQuality(quality, out var qualityNorm))
            {
                messageBox.Show("Error", "Quality must be 'Pass' or 'Rework'.");
                return;
            }

            // Destination
            if (!ValidateDestination(destinationId))
            {
                messageBox.Show("Error", "Destination must be 1, 2, or 3.");
                return;
            }

            // Priority
            if (!ValidatePriority(priority))
            {
                messageBox.Show("Error", "Priority must be between 1 and 100.");
                return;
            }

            // Enabled
            if (!TryParseEnabled(enabledText, out bool enabled))
            {
                messageBox.Show("Error", "Enabled must be 'Yes' or 'No'.");
                return;
            }

            var route = new StationRoute
            {
                StationId = stationId,
                Quality = qualityNorm,     // "Pass" / "Rework"
                Destination = destinationId,   // 1, 2, 3
                Priority = priority,        // 1..100
                Enabled = enabled          // bool -> repo mapea a 0/1
            };

            myStore.StationRoutesRepo.Insert(route);
            messageBox.Show("Info", $"Route added successfully (StationId {stationId} → Dest {destinationId}, {qualityNorm}, Priority {priority}, Enabled {(enabled ? 1 : 0)}).");
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
    public void UpdateStationRoute(int routeId, int stationId, string quality, sbyte destinationId, int priority, string enabledText)
    {
        try
        {
            // Route / Station existen
            if (!RouteExists(routeId))
            {
                messageBox.Show("Error", $"RouteId {routeId} is invalid or not found.");
                return;
            }
            if (!StationExists(stationId))
            {
                messageBox.Show("Error", $"StationId {stationId} is invalid or not found.");
                return;
            }

            // Quality
            if (!ValidateQuality(quality, out var qualityNorm))
            {
                messageBox.Show("Error", "Quality must be 'Pass' or 'Rework'.");
                return;
            }

            // Destination
            if (!ValidateDestination(destinationId))
            {
                messageBox.Show("Error", "Destination must be 1, 2, or 3.");
                return;
            }

            // Priority
            if (!ValidatePriority(priority))
            {
                messageBox.Show("Error", "Priority must be between 1 and 100.");
                return;
            }

            // Enabled
            if (!TryParseEnabled(enabledText, out bool enabled))
            {
                messageBox.Show("Error", "Enabled must be 'Yes' or 'No'.");
                return;
            }

            var route = new StationRoute
            {
                Id = routeId,
                StationId = stationId,
                Quality = qualityNorm,
                Destination = destinationId,
                Priority = priority,
                Enabled = enabled
            };

            myStore.StationRoutesRepo.Update(route);
            messageBox.Show("Info", $"Route {routeId} updated successfully.");
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
    public void DeleteStationRoute(string routeId)
    {
        try
        {
            var id = Convert.ToInt32(routeId);
            myStore.StationRoutesRepo.DeleteByID(id);
            messageBox.Show("Info", $"Route with Id {id} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    private bool ValidateQuality(string quality, out string normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(quality)) return false;

        var q = quality.Trim();
        if (q.Equals("Pass", StringComparison.OrdinalIgnoreCase)) { normalized = "Pass"; return true; }
        if (q.Equals("Rework", StringComparison.OrdinalIgnoreCase)) { normalized = "Rework"; return true; }
        return false;
    }

    private bool ValidateDestination(sbyte destination)
    {
        return destination == 1 || destination == 2 || destination == 3;
    }

    private bool ValidatePriority(int priority)
    {
        return priority > 0 && priority <= 100;
    }

    private bool TryParseEnabled(string enabledText, out bool enabled)
    {
        enabled = false;
        if (string.IsNullOrWhiteSpace(enabledText)) return false;

        if (enabledText.Equals("Yes", StringComparison.OrdinalIgnoreCase)) { enabled = true; return true; }
        if (enabledText.Equals("No", StringComparison.OrdinalIgnoreCase)) { enabled = false; return true; }
        return false;
    }

    private bool StationExists(int stationId)
    {
        try
        {
            // Si tu repo tiene ExistsById, úsalo:
            if (myStore.StationRepo.ExistsById(stationId)) return true;
            // Si no existe el método, asumimos inválido
            return false;
        }
        catch
        {
            // Fallback por si el método no existe en tu implementación actual:
            return stationId > 0;
        }
    }

    private bool RouteExists(int routeId)
    {
        try
        {
            if (myStore.StationRoutesRepo.ExistsById(routeId)) return true;
            return false;
        }
        catch
        {
            return routeId > 0;
        }
    }
}
