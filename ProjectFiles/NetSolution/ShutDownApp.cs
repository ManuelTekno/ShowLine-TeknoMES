#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.ODBCStore;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.EventLogger;
using FTOptix.RAEtherNetIP;
using FTOptix.WebUI;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class ShutDownApp : BaseNetLogic
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
    public void CloseApplication()
    {
        try
        {
            Log.Info("Application", "Closing FactoryTalk Optix application.");
            Environment.Exit(0); // Terminates the application
        }
        catch (Exception e)
        {
            Log.Error("Application", "Error closing application: " + e.Message);
        }
    }

}
