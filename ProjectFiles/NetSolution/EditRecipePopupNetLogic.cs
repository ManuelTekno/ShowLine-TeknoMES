using FTOptix.NetLogic;
using FTOptix.UI;
using NETCode.Entities;
using NETCode.Services;
using System;
using UAManagedCore;
using NETCode.Core;
using FTOptix.WebUI;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Alarm;

public class EditRecipePopupNetLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void UpdateRecipe(string name, string stationName, string variantName , string newName)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(newName))
            {
                messageBox.Show("Error", "Recipe New Name field cannot be blank.");
                return;
            }
            
            var stationId = myStore.StationRepo.GetIdByName(stationName);
            var variantId = myStore.VariantsRepo.GetIdByName(variantName);

            if (stationId == null || variantId == null)
            {
                messageBox.Show("Error", "Station or Variant not found.");
                return;
            }

            var recipe = new Recipe()
            {
                ID = (int)myStore.RecipeRepo.GetIdByName(name),
                Name = newName,
                StationId = stationId.Value,
                VariantId = variantId.Value
            };

            myStore.RecipeRepo.Update(recipe);

            messageBox.Show("Info", $"Recipe '{name}' updated successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", $"Failed to update recipe: {ex.Message}");
        }
    }
}
