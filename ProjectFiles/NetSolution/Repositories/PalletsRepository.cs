using System;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Services;
using UAManagedCore;

namespace NETCode.Repositories;

public class PalletRepository : OptixRepositoryBase<Pallet>, IOptixRepository<Pallet>
{
    public PalletRepository( )
        : base(
            "pallets",
            new string[] { "rfid_tag", "description" },
            (resultSet, row) => new Pallet
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                RfidTag = resultSet[row, 1]?.ToString(),
                Description = resultSet[row, 2]?.ToString()
            }
            
        )
    {
        Log.Info("PalletRepository instance created");
    }

    public void Insert(Pallet entity)
    {
        var values = new object[] { entity.RfidTag, entity.Description };
        base.Insert(entity, values);
    }

    public void Update(Pallet entity)
    {
        string query = $"UPDATE pallets SET rfid_tag = '{entity.RfidTag}', description = '{entity.Description}' WHERE id = {entity.Id}";
        ExecuteQuery(query);
    }

    public bool ExistsById(int id)
    {
        string query = $"SELECT id FROM pallets WHERE id = {id}";
        var resultSet = ExecuteQuery(query);
        return resultSet.GetLength(0) > 0;
    }

    public Pallet FindByRFID(string rfidTag)
    {
        string query = $"SELECT * FROM pallets WHERE RfidTag = '{rfidTag}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null;

        return ParseRow(resultSet, 0);
    }

    private Pallet ParseRow(object[,] resultSet, int row)
    {
        return new Pallet
        {
            Id = Convert.ToInt32(resultSet[row, 0]),
            RfidTag = resultSet[row, 1]?.ToString(),
            Description = resultSet[row, 2]?.ToString()
        };
    }
    public int? GetIdByRFIDTag(string rfidTag)
    {
        string query = $"SELECT id FROM pallets WHERE rfid_tag = '{rfidTag}'";
        var result = ExecuteQuery(query);
        if (result.GetLength(0) == 0)
            return null;

        return Convert.ToInt32(result[0, 0]);
    }

}
