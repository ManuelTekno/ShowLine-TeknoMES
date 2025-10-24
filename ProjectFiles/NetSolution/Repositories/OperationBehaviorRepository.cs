using NETCode.Core;
using NETCode.Entities;
using NETCode.Repositories;
using NETCode.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class OperationBehaviorRepository : OptixRepositoryBase<OperationBehavior>, IOptixRepository<OperationBehavior>
    {
        public OperationBehaviorRepository()
            : base(
                "operations_behavior",
                new string[] { "name", "Description" },
                (resultSet, row) => new OperationBehavior
                {
                    ID = Convert.ToInt32(resultSet[row, 0]),
                    Name = resultSet[row, 1]?.ToString(),
                    Description = resultSet[row, 2]?.ToString()
                }
            )
        {
            Log.Info("OperationBehavior Repository instance created");
        }

        public void Insert(OperationBehavior entity)
        {
            var values = new object[] { entity.Name, entity.Description };
            base.Insert(entity, values);
        }

        public void Update(OperationBehavior entity)
        {
            string query = $"UPDATE operations_behavior SET Name = '{entity.Name}', Description = '{entity.Description}' WHERE id = {entity.ID}";
            ExecuteQuery(query);
        }
        public int? GetIdByName(string name)
        {
            string query = $"SELECT id FROM operations_behavior WHERE name = '{name}'";
            var resultSet = ExecuteQuery(query);

            if (resultSet.GetLength(0) == 0)
                return null;

            return Convert.ToInt32(resultSet[0, 0]);
        }

    }

}
