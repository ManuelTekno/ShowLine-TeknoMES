using System;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class ProductionUnitOperationResultRepository : OptixRepositoryBase<ProductionUnitOperationResult>, IOptixRepository<ProductionUnitOperationResult>
    {
        public ProductionUnitOperationResultRepository()
            : base(
                "production_unit_operation_results",
                new string[] { "unit_id", "operation_id", "name", "parameter","value", "result"},
                (resultSet, row) => new ProductionUnitOperationResult
                {
                    Id = Convert.ToInt32(resultSet[row, 0]),
                    UnitId = Convert.ToInt32(resultSet[row, 1]),
                    OperationId = Convert.ToInt32(resultSet[row, 2]),
                    Name = resultSet[row, 3]?.ToString(),
                    Parameter = resultSet[row, 4]?.ToString(),
                    Value = resultSet[row, 5]?.ToString(),
                    Result = resultSet[row, 6]?.ToString(),
                })
        {
            Log.Info("ProductionUnitOperationResultRepository instance created");
        }

        public void Insert(ProductionUnitOperationResult entity)
        {
            var values = new object[]
            {
                entity.UnitId,
                entity.OperationId,
                entity.Name,
                entity.Parameter,
                entity.Value,
                entity.Result,
            };

            base.Insert(entity, values);
        }

        public void Update(ProductionUnitOperationResult entity)
        {
            string query = $@"
        UPDATE production_unit_operation_results SET 
            unit_result_id = {entity.UnitId},
            operation_id = {entity.OperationId},
            name = '{entity.Name?.Replace("'", "''")}',
            parameters = '{entity.Parameter?.Replace("'", "''")}',
            value = '{entity.Value?.Replace("'", "''")}',
            result = '{entity.Result?.Replace("'", "''")}'
        WHERE id = {entity.Id};";


            ExecuteQuery(query);
        }

    }
}
