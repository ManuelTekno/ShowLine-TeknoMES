#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using NETCode.Entities;
using NETCode.Services;
using NETCode.Core;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;
#endregion

public class CreateUserPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    public override void Stop()
    {
        // Código opcional para limpieza
    }

    [ExportMethod]
    public void CreateNewUser(string username, string password, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                messageBox.Show("Warning", "Username, password, and role must be provided.");
                return;
            }

            int? existingUserId = myStore.UsersRepo.GetIdByName(username);
            if (existingUserId.HasValue)
            {
                messageBox.Show("Warning", "This username is already taken.");
                return;
            }

            var newUser = new Users
            {
                UserName = username.Trim(),
                UserPassword = password.Trim(),
                Rol = role.Trim(),
                DateCreated = DateTime.Now
            };

            myStore.UsersRepo.Insert(newUser);

            messageBox.Show("Success", "User created successfully.");
        }
        catch (Exception e)
        {
            Log.Error("CreateUserLogic", $"Error creating user: {e.Message}");
            messageBox.Show("Error", $"Unexpected error: {e.Message}");
        }
    }
}
