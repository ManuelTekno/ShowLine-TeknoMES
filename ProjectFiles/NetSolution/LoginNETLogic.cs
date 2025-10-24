#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using NETCode.Services;
using NETCode.Core;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class LoginNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;
    private IUAVariable currentUserVar;
    private IUAVariable currentRoleVar;
    private IUAVariable loggedInVar;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
        currentUserVar = Project.Current.GetVariable("Model/Users_Management/Current_User_Name");
        currentRoleVar = Project.Current.GetVariable("Model/Users_Management/Current_User_Role");
        loggedInVar = Project.Current.GetVariable("Model/Users_Management/Logged_In");
        if (currentUserVar == null || currentRoleVar == null || loggedInVar == null)
        {
            Log.Error("LoginNetLogic", "One or more session variables not found at startup.");
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void PerformLoginDataBase(string username, string password, NodeId panelLoaderID, NodeId homeScreenID)
    {

        try
        {
            // Prevent login if username or password is empty
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                messageBox.Show("Warning", "Username and password are required.");
                return;
            }
            // Validate user credentials directly in DB
            string userRole = myStore.UsersRepo.ValidateUserCredentials(username, password);

            if (string.IsNullOrEmpty(userRole))
            {
                messageBox.Show("Warning", "Incorrect username or password.");
                return;
            }

            Log.Info("LoginButtonLogic", $"User '{username}' authenticated with role: {userRole}");

            // Update session variables
            currentUserVar.Value = username;
            currentRoleVar.Value = userRole;
            loggedInVar.Value = true;

            // Update last login date
            var userToUpdate = myStore.UsersRepo.GetById(myStore.UsersRepo.GetIdByName(username) ?? -1);
            if (userToUpdate != null)
            {
                userToUpdate.LastLoginDate = DateTime.Now;
                myStore.UsersRepo.Update(userToUpdate);
            }

            // Navigate to home screen
            var panelLoader = InformationModel.Get<PanelLoader>(panelLoaderID);
            if (panelLoader == null)
            {
                messageBox.Show("Error", "System error: Unable to navigate to HOME screen.");
                return;
            }

            panelLoader.ChangePanel(homeScreenID);
        }
        catch (Exception e)
        {
            Log.Error("LoginButtonLogic", $"Error during login: {e.Message}");
            messageBox.Show("Error", "An unexpected error occurred.");
        }
    }
}
