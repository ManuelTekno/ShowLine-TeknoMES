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

public class VariantsRepository : OptixRepositoryBase<Variants>, IOptixRepository<Variants>
{
    public VariantsRepository()
        : base(
            "variants",
            new string[] { "name", "serial_code", "description" },
            (resultSet, row) => new Variants
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                Name = resultSet[row, 1]?.ToString(),
                SerialCode = resultSet[row, 2]?.ToString(),
                Description = resultSet[row, 3]?.ToString()
            }
            
        )
    {
        Log.Info("Variants Repository instance created");
    }

    public void Insert(Variants entity)
    {
        var values = new object[] { entity.Name, entity.SerialCode, entity.Description };
        base.Insert(entity, values);
    }

    public void Update(Variants entity)
    {
        string query = $"UPDATE variants SET name = '{entity.Name}', serial_code = '{entity.SerialCode}', description = '{entity.Description}' WHERE id = {entity.Id}";
        ExecuteQuery(query);
    }

    public int? GetIdByName(string name)
    {
        string query = $"SELECT id FROM variants WHERE name = '{name}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null;

        return Convert.ToInt32(resultSet[0, 0]);
    }

    public int? GetIdBySerialCode(string serialCode)
    {
        string query = $"SELECT id FROM variants WHERE serial_code = '{serialCode}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null;

        return Convert.ToInt32(resultSet[0, 0]);
    }

    public Variants FindByPartialSerialMatch(string validationCode)
    {
        if (string.IsNullOrWhiteSpace(validationCode))
            return null;

        validationCode = validationCode.Trim().ToUpperInvariant();

        var allVariants = GetAll();

        foreach (var variant in allVariants)
        {
            if (string.IsNullOrWhiteSpace(variant.SerialCode))
                continue;

            var code = variant.SerialCode.Trim().ToUpperInvariant();

            // Si empieza con el color como prefijo (ej. GRAY, RED, GREEN)
            if (validationCode.StartsWith(code))
            {
                Log.Info($"[VariantsRepo] Match found: '{validationCode}' starts with '{code}'");
                return variant;
            }
        }

        Log.Warning($"[VariantsRepo] No variant found matching prefix in '{validationCode}'");
        return null;
    }


}
