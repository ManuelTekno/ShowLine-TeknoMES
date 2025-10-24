#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.ODBCStore;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.EventLogger;
using FTOptix.RAEtherNetIP;
using FTOptix.WebUI;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class ToogleConfigMenu : BaseNetLogic
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

    public void ToogleConfigPanel()
    {

        var panel = Owner.Owner.GetObject("panelConfig") as Panel;
        panel.Visible = !panel.Visible; // Alternar visibilidad

    }
}
