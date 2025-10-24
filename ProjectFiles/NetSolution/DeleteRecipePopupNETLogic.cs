using FTOptix.NetLogic;
using NETCode.Entities;
using FTOptix.UI;
using System;
using UAManagedCore;
using NETCode.Core;
using NETCode.Services;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

public class DeleteRecipePopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void DeleteRecipe(string recipeId)
    {
        try
        {
            var id = Convert.ToInt32(recipeId);

            // Verifica si existen operaciones relacionadas a esta receta
            bool hasOperations = myStore.OperationRepo.ExistsByRecipeId(id);

            if (hasOperations)
            {
                // Elimina operaciones relacionadas
                myStore.OperationRepo.DeleteByRecipeId(id);
            }

            // Elimina la receta
            myStore.RecipeRepo.DeleteByID(id);

            messageBox.Show("Info", $"Recipe with Id {id} deleted successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }
}
