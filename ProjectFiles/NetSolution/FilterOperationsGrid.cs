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

public class FilterOperationsGrid : BaseNetLogic
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
    public void Filter(string stationName, string variantName)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(variantName))
            conditions.Add($"recipe_name = '{variantName.Replace("'", "''")}'");

        if (!string.IsNullOrWhiteSpace(stationName))
            conditions.Add($"station_name = '{stationName.Replace("'", "''")}'");

        string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        string queryExecuted = $"SELECT * FROM view_operations_details {whereClause}";

        var varNode = Owner.GetVariable("QueryInput");
        if (varNode != null)
        {
            varNode.Value = new UAValue(queryExecuted);
        }
    }

}
