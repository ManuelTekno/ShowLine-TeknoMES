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
using NETCode.Core;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

#endregion

public class DeleteOperationPopupNETLogic : BaseNetLogic
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
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void DeleteOperationAndReorder(string operationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(operationId) || !int.TryParse(operationId, out int opId))
            {
                messageBox.Show("Error", "Invalid operation ID.");
                return;
            }

            // Get Operation
            var operation = myStore.OperationRepo.GetById(opId);
            if (operation == null)
            {
                messageBox.Show("Error", $"Operation with ID {opId} not found.");
                return;
            }

            //Copy recipe ID
            int recipeId = operation.RecipeID;

            // Delete operations
            myStore.OperationRepo.DeleteByID(opId);

            // Get operations lis reordered
            var operations = myStore.OperationRepo.GetByRecipeIdOrdered(recipeId);

            // Reasing index of each operation starting 0
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Index != i) // Solo actualizar si es necesario
                {
                    operations[i].Index = i;
                    myStore.OperationRepo.Update(operations[i]);
                }
            }

            messageBox.Show("Info", "Operation deleted and indices reordered.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

}
