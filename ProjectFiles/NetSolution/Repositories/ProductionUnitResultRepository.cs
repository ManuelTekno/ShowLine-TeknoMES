using System;
using System.Collections.Generic;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories;

public class ProductionUnitResultRepository : OptixRepositoryBase<ProductionUnitResult>, IOptixRepository<ProductionUnitResult>
{
    public ProductionUnitResultRepository()
        : base(
            "production_unit_results",
            new string[] { "unit_id", "station_id", "cycle_time", "status", "finished_at" },
            (resultSet, row) => new ProductionUnitResult
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                UnitId = Convert.ToInt32(resultSet[row, 1]),
                StationId = Convert.ToInt32(resultSet[row, 2]),
                CycleTime = resultSet[row, 3] is DBNull ? null : (float?)Convert.ToSingle(resultSet[row, 3]),
                Status = resultSet[row, 4]?.ToString(),
                FinishedAt = resultSet[row, 5] is DBNull ? null : (DateTime?)Convert.ToDateTime(resultSet[row, 5])
            })
    {
        Log.Info("ProductionUnitResultRepository instance created");
    }

    public void Insert(ProductionUnitResult entity)
    {
        var values = new object[]
        {
            entity.UnitId,
            entity.StationId,
            entity.CycleTime,
            entity.Status,
            entity.FinishedAt
        };

        base.Insert(entity, values);
    }

    public void Update(ProductionUnitResult entity)
    {
        string query = $@"
        UPDATE production_unit_results SET 
            unit_id = {entity.UnitId},
            station_id = {entity.StationId},
            cycle_time = {(entity.CycleTime.HasValue ? entity.CycleTime.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL")},
            status = '{entity.Status?.Replace("'", "''")}',
            finished_at = {(entity.FinishedAt.HasValue ? $"'{entity.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")}'" : "NULL")}
        WHERE id = {entity.Id};";

        ExecuteQuery(query);
    }
}
