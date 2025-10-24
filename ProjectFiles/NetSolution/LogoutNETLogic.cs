#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using NETCode.Core;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class LogoutNETLogic : BaseNetLogic
{
    private ProjectFolder project;
    private MessageBoxService messageBox;
    private IUAVariable currentUserVar;
    private IUAVariable currentRoleVar;
    private IUAVariable loggedInVar;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        messageBox = new MessageBoxService(Owner);
        project = Project.Current;

        // ✅ Retrieve global session variables
        currentUserVar = project.GetVariable("Model/Users_Management/Current_User_Name");
        currentRoleVar = project.GetVariable("Model/Users_Management/Current_User_Role");
        loggedInVar = project.GetVariable("Model/Users_Management/Logged_In");

        if (currentUserVar == null || currentRoleVar == null || loggedInVar == null)
        {
            messageBox.Show("Error", "System error: Session variables not found.");
            return;
        }


    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void PerformLogout(NodeId panelLoaderID, NodeId loginScreenID)
    {

        try
        {
            // ✅ Clear session variables
            currentUserVar.Value = "";
            currentRoleVar.Value = "";
            loggedInVar.Value = false;
            messageBox.Show("Info", "You have been logged out successfully.");

            // ✅ Retrieve the PanelLoader
            var panelLoader = InformationModel.Get<PanelLoader>(panelLoaderID);
            if (panelLoader == null)
            {
                messageBox.Show("Error", "System error: Unable to navigate to login screen.");
                return;
            }

            // ✅ Navigate to Login screen
            panelLoader.ChangePanel(loginScreenID);
        }
        catch (Exception e)
        {
            messageBox.Show("Error", $"Unexpected error: {e.Message}");
        }
    }
}
