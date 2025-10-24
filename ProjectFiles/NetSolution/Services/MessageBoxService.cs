using FTOptix.HMIProject;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace NETCode.Core;

public class MessageBoxService
{
    private readonly DialogType messageDialog;
    private readonly IUANode parentContainer;
    public MessageBoxService(IUANode owner)
    {
        messageDialog = (DialogType)Project.Current.Get("UI/Popups/MessagePopup");

        if (owner != null)
        {
            parentContainer = owner.Owner?.Owner as BaseUIObject;
        }
        else
        {
            Log.Error("MessageBoxService", "Owner is null!");
        }
    }

    public void Show(string messageType, string message)
    {
        if (messageDialog == null || parentContainer == null)
        {
            Log.Error("MessageBoxService", "Dialog or parent container not found.");
            return;
        }

        // Find UI elements inside the popup
        var msgLabel = messageDialog.FindObject("Message") as Label;
        var errorIcon = messageDialog.FindObject("Error") as Image;
        var infoIcon = messageDialog.FindObject("Info") as Image;
        var warningIcon = messageDialog.FindObject("Warning") as Image;

        // Set message text
        if (msgLabel != null)
            msgLabel.Text = message;

        // Control icon visibility based on type
        if (errorIcon != null && infoIcon != null && warningIcon != null)
        {
            errorIcon.Visible = messageType == "Error";
            warningIcon.Visible = messageType == "Warning";
            infoIcon.Visible = messageType == "Info";
        }

        // Open dialog
        UICommands.OpenDialog(parentContainer, messageDialog);
    }
}
