using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using NETCode.Services;
using NETCode.Core;
using FTOptix.HMIProject;
using NETCode.Entities;

public class CreateRecipeOperationPopupNETLogic : BaseNetLogic
{
    private OptixDBService myStore;
    private MessageBoxService messageBox;
    private const int MaxPartsInValueString = 32;

    public override void Start()
    {
        myStore = OptixDBService.GetInstance();
        messageBox = new MessageBoxService(Owner);
    }

    [ExportMethod]
    public void CreateOperation(string recipeId, string operationTypeId, string behaviorId, string valueString, string valueReal, string description)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(recipeId) || string.IsNullOrEmpty(operationTypeId) || string.IsNullOrEmpty(behaviorId))
            {
                messageBox.Show("Error", "Recipe, operation type and behavior are required.");
                return;
            }

            // Convert types
            int recipe = Convert.ToInt32(recipeId);
            int operationType = Convert.ToInt32(operationTypeId);
            int behavior = Convert.ToInt32(behaviorId);

            // Validate recipe existence
            if (!myStore.RecipeRepo.ExistsById(recipe))
            {
                messageBox.Show("Error", $"No recipe found with Id {recipe}.");
                return;
            }

            // Parse optional values
            string finalValueString = valueString?.Trim();
            float? finalValueReal = null;
            if (!string.IsNullOrWhiteSpace(valueReal))
            {
                if (float.TryParse(valueReal, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
                {
                    finalValueReal = parsedValue;
                }
                else
                {
                    messageBox.Show("Error", $"Invalid float value: '{valueReal}'");
                    return;
                }
            }

            string finalDescription = description?.Trim();

            // Get next index
            int newSequence = myStore.OperationRepo.GetNextSequenceOrderByRecipeId(recipe);

            // Create operation
            var newOperation = new Operation
            {
                RecipeID = recipe,
                OperationTypeID = operationType,
                BehaviorID = behavior,
                Index = newSequence,
                ValueString = finalValueString,
                ValueReal = finalValueReal,
                Description = finalDescription
            };

            myStore.OperationRepo.Insert(newOperation);

            messageBox.Show("Info", $"Operation '{finalDescription}' added successfully with sequence {newSequence}.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }
[ExportMethod]
    public void AddPartToValueString(string partNumber, NodeId textBoxNodeId)
    {
        try
        {
            var part = NormalizePart(partNumber);
            if (string.IsNullOrEmpty(part))
            {
                messageBox.Show("Error", "Part number cannot be empty.");
                return;
            }

            var textBox = InformationModel.Get<TextBox>(textBoxNodeId);
            if (textBox == null)
                throw new Exception("Invalid TextBox NodeId.");

            var list = GetCurrentParts(textBox);
            if (list.Contains(part))
            {
                messageBox.Show("Info", $"Part '{part}' is already in the list.");
                return;
            }

            if (list.Count >= MaxPartsInValueString)
            {
                messageBox.Show("Error", $"You reached the limit of {MaxPartsInValueString} parts.");
                return;
            }

            list.Add(part);
            list = list
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            SetCurrentParts(textBox, list);
            messageBox.Show("Info", $"Added '{part}'.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    [ExportMethod]
    public void RemovePartFromValueString(string partNumber, NodeId textBoxNodeId)
    {
        try
        {
            var part = NormalizePart(partNumber);
            if (string.IsNullOrEmpty(part))
            {
                messageBox.Show("Error", "Part number cannot be empty.");
                return;
            }

            var textBox = InformationModel.Get<TextBox>(textBoxNodeId);
            if (textBox == null)
                throw new Exception("Invalid TextBox NodeId.");

            var list = GetCurrentParts(textBox);
            int before = list.Count;
            list = list.Where(p => !p.Equals(part, StringComparison.OrdinalIgnoreCase)).ToList();

            if (list.Count == before)
            {
                messageBox.Show("Info", $"Part '{part}' was not in the list.");
                return;
            }

            SetCurrentParts(textBox, list);
            messageBox.Show("Info", $"Removed '{part}'.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    [ExportMethod]
    public void RemoveLastPartFromValueString(NodeId textBoxNodeId)
    {
        try
        {
            var textBox = InformationModel.Get<TextBox>(textBoxNodeId);
            if (textBox == null)
                throw new Exception("Invalid TextBox NodeId.");

            var list = GetCurrentParts(textBox);
            if (list.Count == 0)
            {
                messageBox.Show("Info", "List is already empty.");
                return;
            }

            var removed = list.Last();
            list.RemoveAt(list.Count - 1);
            SetCurrentParts(textBox, list);
            messageBox.Show("Info", $"Removed last part: '{removed}'.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    [ExportMethod]
    public void ClearValueString(NodeId textBoxNodeId)
    {
        try
        {
            var textBox = InformationModel.Get<TextBox>(textBoxNodeId);
            if (textBox == null)
                throw new Exception("Invalid TextBox NodeId.");

            textBox.Text = string.Empty;
            messageBox.Show("Info", "List cleared.");
        }
        catch (Exception ex)
        {
            messageBox.Show("Error", ex.Message);
        }
    }

    // =======================================================
    // HELPERS
    // =======================================================
    private List<string> GetCurrentParts(TextBox textBox)
    {
        var csv = textBox.Text ?? "";
        return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(NormalizePart)
                  .Where(s => !string.IsNullOrEmpty(s))
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .ToList();
    }

    private void SetCurrentParts(TextBox textBox, List<string> parts)
    {
        textBox.Text = string.Join(",", parts);
    }

    private string NormalizePart(string input)
    {
        var s = (input ?? "").Trim();
        return string.IsNullOrEmpty(s) ? "" : s.ToUpperInvariant();
    }
}
