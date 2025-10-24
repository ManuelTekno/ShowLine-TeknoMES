#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.WebUI;
using FTOptix.ODBCStore;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class FilterProductionUnitsHistorical : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Filter(string stationName, string palletID, string serialCode)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(palletID))
            conditions.Add($"Pallet_ID = '{palletID.Replace("'", "''")}'");

        if (!string.IsNullOrWhiteSpace(stationName))
            conditions.Add($"Current_Station = '{stationName.Replace("'", "''")}'");

        if (!string.IsNullOrWhiteSpace(serialCode))
            conditions.Add($"Serial_Code = '{serialCode.Replace("'", "''")}'");

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string queryExecuted = $"SELECT * FROM production_units_historical {whereClause}";

        var varNode = Owner.GetVariable("QueryInput");
        if (varNode != null)
        {
            varNode.Value = new UAValue(queryExecuted);
        }
    }

}
