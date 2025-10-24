using System;
using System.Collections.Generic;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class OperationRepository : OptixRepositoryBase<Operation>, IOptixRepository<Operation>
    {
        public OperationRepository()
            : base(
                "operations",
                new string[] { "recipe_id", "operation_type_id", "behavior_id", "index_number", "value_string", "value_real", "description"},
                (resultSet, row) => new Operation
                {
                    ID = Convert.ToInt32(resultSet[row, 0]),
                    RecipeID = Convert.ToInt32(resultSet[row, 1]),
                    OperationTypeID = Convert.ToInt32(resultSet[row, 2]),
                    BehaviorID = Convert.ToInt32(resultSet[row, 3]),
                    Index = Convert.ToInt32(resultSet[row, 4]),
                    ValueString = resultSet[row, 5]?.ToString(),
                    ValueReal = resultSet[row, 6] is DBNull ? 0 : (float)Convert.ToSingle(resultSet[row, 6]),
                    Description = resultSet[row, 7]?.ToString()
                }
            )
        {
            Log.Info("OperationRepository instance created");
        }

        public void Insert(Operation entity)
        {
            var values = new object[]
            {
                entity.RecipeID,
                entity.OperationTypeID,
                entity.BehaviorID,
                entity.Index,
                entity.ValueString,
                entity.ValueReal,
                entity.Description,
                
            };
            base.Insert(entity, values);
        }

        public void Update(Operation entity)
        {
            string query = $"UPDATE operations SET " +
                           $"recipe_id = {entity.RecipeID}, " +
                           $"operation_type_id = {entity.OperationTypeID}, " +
                           $"behavior_id = {entity.BehaviorID}, " +
                           $"index_number = {entity.Index}, " +
                           $"value_string = '{entity.ValueString?.Replace("'", "''")}', " +
                           $"value_real = {entity.ValueReal?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"}, " +
                           $"description = '{entity.Description?.Replace("'", "''")}' " +
                           $"WHERE id = {entity.ID}";

            ExecuteQuery(query);
        }

        public int GetNextSequenceOrderByRecipeId(int? recipeId)
        {
            string query = $"SELECT MAX(index_number) FROM operations WHERE recipe_id = {recipeId}";
            var resultSet = ExecuteQuery(query);

            if (resultSet == null)
            {
                Log.Warning("[GetNextSequenceOrderByRecipeId] resultSet is null. Returning 0.");
                return 0;
            }

            if (resultSet.GetLength(0) == 0)
            {
                Log.Info("[GetNextSequenceOrderByRecipeId] No rows returned. Returning 0.");
                return 0;
            }

            var raw = resultSet[0, 0];
            if (raw == null || raw == DBNull.Value)
            {
                Log.Info("[GetNextSequenceOrderByRecipeId] MAX(index_number) is NULL. Returning 0.");
                return 0;
            }

            int currentMax = Convert.ToInt32(raw);
            Log.Info($"[GetNextSequenceOrderByRecipeId] currentMax = {currentMax}. Returning {currentMax + 1}.");
            return currentMax + 1;
        }



        public List<Operation> GetByRecipeId(int recipeId)
        {
            string query = $@"
        SELECT 
            id, 
            recipe_id, 
            operation_type_id, 
            behavior_id, 
            index_number, 
            value_string, 
            value_real, 
            description 
        FROM operations 
        WHERE recipe_id = {recipeId} 
        ORDER BY index_number ASC";

            var resultSet = ExecuteQuery(query);
            var operations = new List<Operation>();

            for (int i = 0; i < resultSet.GetLength(0); i++)
            {
                operations.Add(new Operation
                {
                    ID = Convert.ToInt32(resultSet[i, 0]),
                    RecipeID = Convert.ToInt32(resultSet[i, 1]),
                    OperationTypeID = Convert.ToInt32(resultSet[i, 2]),
                    BehaviorID = Convert.ToInt32(resultSet[i, 3]),
                    Index = Convert.ToInt32(resultSet[i, 4]),
                    ValueString = resultSet[i, 5]?.ToString(),
                    ValueReal = resultSet[i, 6] is DBNull ? null : (float?)Convert.ToSingle(resultSet[i, 6]),
                    Description = resultSet[i, 7]?.ToString()
                });
            }

            return operations;
        }

        public void DeleteByRecipeId(int? recipeId)
        {
            string query = $"DELETE FROM operations WHERE recipe_id = {recipeId}";
            ExecuteQuery(query);
        }

        public bool ExistsByRecipeId(int? recipeId)
        {
            string query = $"SELECT * FROM operations WHERE recipe_id = {recipeId}";
            var resultSet = ExecuteQuery(query);
            return resultSet.GetLength(0) > 0;
        }

        public void DeleteByIdAndSequenceOrder(int id, int operationIndex)
        {
            if (id <= 0 || operationIndex <= 0)
                throw new ArgumentException("Id and Sequence Order must be greater than zero.");

            string query = $"DELETE FROM operations WHERE id = {id} AND index_number = {operationIndex}";
            ExecuteQuery(query);
        }

        public int? GetOperationIdByFields(int? operationTypeId, int? recipeId, int? operationIndex)
        {
            string query =
                $"SELECT id FROM operations WHERE operation_type_id = {operationTypeId} " +
                $"AND recipe_id = {recipeId} AND index_number = {operationIndex}";

            var resultSet = ExecuteQuery(query);
            if (resultSet.GetLength(0) == 0 || resultSet[0, 0] == DBNull.Value)
                return null;
            return Convert.ToInt32(resultSet[0, 0]);
        }

        public int? GetLastInsertedOperationId(int? recipeId)
        {
            if (recipeId == null)
                throw new ArgumentNullException(nameof(recipeId), "Recipe Id cannot be null.");

            string query = $"SELECT MAX(id) FROM operations WHERE recipe_id = {recipeId}";
            var resultSet = ExecuteQuery(query);
            if (resultSet.GetLength(0) == 0 || resultSet[0, 0] == DBNull.Value)
                return null;
            return Convert.ToInt32(resultSet[0, 0]);
        }
        public List<Operation> GetByRecipeIdOrdered(int recipeId)
        {
            string query = $"SELECT * FROM operations WHERE recipe_id = {recipeId} ORDER BY index_number ASC";
            var resultSet = ExecuteQuery(query);
            var operations = new List<Operation>();

            for (int i = 0; i < resultSet.GetLength(0); i++)
            {
                operations.Add(MapFunc(resultSet, i));
            }

            return operations;
        }

        // Put these INSIDE OperationRepository
        public Operation GetFirstPickToLightAny()
        {
            const int PTL_TYPE_ID = 7;

            string query =
                "SELECT *" +
                "FROM operations " +
                "WHERE operation_type_id = " + PTL_TYPE_ID + " " +
                "ORDER BY index_number ASC, id ASC";

            var rs = ExecuteQuery(query);
            if (rs == null || rs.GetLength(0) == 0) return null;

            int row = 0; // take the first row from the ordered set
            return new Operation
            {
                ID = Convert.ToInt32(rs[row, 0]),
                RecipeID = Convert.ToInt32(rs[row, 1]),
                OperationTypeID = Convert.ToInt32(rs[row, 2]),
                BehaviorID = Convert.ToInt32(rs[row, 3]),
                Index = Convert.ToInt32(rs[row, 4]),
                ValueString = rs[row, 5]?.ToString(),
                ValueReal = rs[row, 6] is DBNull ? (float?)null : Convert.ToSingle(rs[row, 6]),
                Description = rs[row, 7]?.ToString()

            };
        }



    }
}
