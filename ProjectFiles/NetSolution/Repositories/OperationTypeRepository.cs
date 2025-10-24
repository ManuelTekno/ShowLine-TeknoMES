using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Services;
using UAManagedCore;

namespace NETCode.Repositories;

public class OperationTypeRepository : OptixRepositoryBase<OperationType>, IOptixRepository<OperationType>
{
    public OperationTypeRepository()
        : base(
            "operations_type",
            new string[] { "Name", "Description" },
            (resultSet, row) => new OperationType
            {
                ID = Convert.ToInt32(resultSet[row, 0]),
                Name = resultSet[row, 1].ToString(),
                Description = resultSet[row, 2]?.ToString()
            }
            
        )
    { Log.Info("Operation Type Repository instance created"); }

    public void Insert(OperationType entity)
    {
        var values = new object[]
        {
            entity.Name,
            entity.Description
        };
        base.Insert(entity, values);
    }

    public void Update(OperationType entity)
    {
        string query = $"UPDATE operations_type SET Name = '{entity.Name}', Description = '{entity.Description}' WHERE id = {entity.ID}";
        ExecuteQuery(query);
    }

    public int? GetIdByName(string name)
    {
        string query = $"SELECT id FROM operations_type WHERE Name = '{name}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null; // Not found

        return Convert.ToInt32(resultSet[0, 0]);
    }


}
