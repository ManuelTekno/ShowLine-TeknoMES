using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;
using System;
using UAManagedCore;

namespace NETCode.Stations.Operations;

public class UserConfirmBehavior : IOperationBehavior
{
    // Puedes cambiar el nombre del tag si ya tienes uno estándar en tu UI/PLC
    private static string BuildConfirmTag(int index) => $"To/Results/{index}/UserConfirm";

    public OperationResult Execute(Station_Base context, Operation operation)
    {
        try
        {
            string confirmTag = BuildConfirmTag(operation.Index);
            bool confirmed = context.ReadBoolTag(confirmTag);

            Log.Info($"[{context._stationTag}] [UserConfirm] Read '{confirmTag}' = {confirmed}");

            if (!confirmed)
            {
                // Aún no hay confirmación del usuario → seguimos esperando
                return OperationResult.Waiting;
            }

            // Confirmado por el usuario → pasó
            return OperationResult.Passed;
        }
        catch (Exception ex)
        {
            Log.Error($"[{context._stationTag}] [UserConfirm] Error: {ex.Message}");
            return OperationResult.Failed;
        }
    }
}
