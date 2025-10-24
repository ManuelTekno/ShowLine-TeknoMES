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

public class CreateRecipePopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void InsertRecipe(string name, string stationName, string variantName)
    {
        try
        {
            myStore = OptixDBService.GetInstance();
            messageBox = new MessageBoxService(Owner);

            if (string.IsNullOrWhiteSpace(name))
            {
                messageBox.Show("Error", "Recipe Name field cannot be blank.");
                return;
            }

            if (myStore.RecipeRepo == null || myStore.StationRepo == null || myStore.VariantsRepo == null)
            {
                messageBox.Show("Error", "One or more repositories are not initialized.");
                return;
            }

            var recipeExists = myStore.RecipeRepo.GetIdByName(name);
            if (recipeExists > 0)
            {
                messageBox.Show("Error", $"Recipe '{name}' already exists. Try another name.");
                return;
            }

            var stationId = myStore.StationRepo.GetIdByName(stationName);
            var variantId = myStore.VariantsRepo.GetIdByName(variantName);

            if (stationId == null || variantId == null)
            {
                messageBox.Show("Error", "Station or Variant not found.");
                return;
            }

            var recipe = new Recipe
            {
                Name = name,
                StationId = stationId.Value,
                VariantId = variantId.Value
            };

            myStore.RecipeRepo.Insert(recipe);

            messageBox.Show("Info", $"Recipe '{name}' created successfully.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", $"Failed to create recipe: {ex.Message}");
        }
    }
}
