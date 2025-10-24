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

public class FilterUnitResults : BaseNetLogic
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
    public void Filter(string stationName, string serialCode)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(stationName))
            conditions.Add($"station_name = '{stationName.Replace("'", "''")}'");

        if (!string.IsNullOrWhiteSpace(serialCode))
            conditions.Add($"serial_code = '{serialCode.Replace("'", "''")}'");

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string queryExecutedStations = $"SELECT * FROM view_station_results {whereClause}";
        string queryExecutedOperations = $"SELECT * FROM view_operation_results {whereClause}";

        var varNode1 = Owner.GetVariable("QueryInputStations");
        var varNode2 = Owner.GetVariable("QueryInputOperations");

        if (varNode1 != null && varNode2 != null)
        {
            varNode1.Value = new UAValue(queryExecutedStations);
            varNode2.Value = new UAValue(queryExecutedOperations);
        }

        else Log.Error("Query Nodes Not Found At Unit Results Screen");
    }

}
