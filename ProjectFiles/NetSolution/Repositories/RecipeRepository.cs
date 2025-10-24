using NETCode.Core;
using NETCode.Repositories;
using NETCode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;
using NETCode.Services;

namespace NETCode.Repositories;

public class RecipeRepository : OptixRepositoryBase<Recipe>, IOptixRepository<Recipe>
{
    public RecipeRepository()
        : base(
            "recipes",
            new string[] { "station_id", "variant_id", "name" },
            (resultSet, row) => new Recipe
            {
                ID = Convert.ToInt32(resultSet[row, 0]),
                StationId = Convert.ToInt32(resultSet[row, 1]),
                VariantId = Convert.ToInt32(resultSet[row, 2]),
                Name = resultSet[row, 3]?.ToString()
            }
            
        )
    {
        Log.Info("Recipe Repository instance created");
    }

    public void Insert(Recipe entity)
    {
        var values = new object[] { entity.StationId, entity.VariantId, entity.Name };
        base.Insert(entity, values);
    }

    public void Update(Recipe entity)
    {
        string query = $"UPDATE recipes SET station_id = {entity.StationId}, variant_id = {entity.VariantId}, name = '{entity.Name}' WHERE id = {entity.ID}";
        ExecuteQuery(query);
    }
    public int? GetIdByName(string name)
    {
        string query = $"SELECT id FROM recipes WHERE name = '{name}'";
        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null;

        return Convert.ToInt32(resultSet[0, 0]);
    }
    public bool ExistsById(int recipeId)
    {
        string query = $"SELECT id FROM recipes WHERE id = {recipeId}";
        var resultSet = ExecuteQuery(query);

        return resultSet.GetLength(0) > 0;
    }

    public Recipe FindByVariantAndStation(int variantId, int stationId)
    {
        string query = $"SELECT id, station_id, variant_id, name " +
                       $"FROM recipes " +
                       $"WHERE variant_id = {variantId} AND station_id = {stationId}";

        var resultSet = ExecuteQuery(query);

        if (resultSet.GetLength(0) == 0)
            return null;

        return new Recipe
        {
            ID = Convert.ToInt32(resultSet[0, 0]),
            StationId = Convert.ToInt32(resultSet[0, 1]),
            VariantId = Convert.ToInt32(resultSet[0, 2]),
            Name = resultSet[0, 3]?.ToString(),
        };
    }

}
