using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Core;

/// <summary>
/// Defines the structure and behavior for a MES station.
/// </summary>
public interface IStation
{
    /// Called when the pallet arrives. Load operations from the database.
    /// Clears any previous data if necessary.
    void Initialize();

    /// Verifies if the unit or product at the station matches the expected identifier.
    /// Usually checks barcodes, serial numbers, or model types.
    void CheckUnit();

    /// Registers the unit in the database.
    /// This method is executed in the first station (dependency equals 0) to create an entry that will be updated by subsequent stations.
    void RegisterUnit();

    /// Executes the operations assigned to the station.
    /// Reads each operation from the UDT and applies the corresponding business logic.
    void ExecuteOperations();

    /// Called when the pallet is ready to leave the station.
    /// This is where the results should be written into the database or sent to an API.
    void SaveUnitResults();
}

