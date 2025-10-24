using System;
using System.Collections.Generic;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class PickToLightScheduleRepository : OptixRepositoryBase<PickToLightSchedule>, IOptixRepository<PickToLightSchedule>
    {
        public PickToLightScheduleRepository()
            : base(
                "pick_to_light_schedule",
                // Columns used for INSERT/UPDATE (PK excluded)
                new[] { "sequence_no", "payload_csv", "status" },
                // Table column order now:
                // id(0), sequence_no(1), payload_csv(2), status(3), updated_at(4)
                (resultSet, row) => new PickToLightSchedule
                {
                    Id = Convert.ToInt32(resultSet[row, 0]),
                    SequenceNo = Convert.ToInt32(resultSet[row, 1]),
                    PayloadCsv = resultSet[row, 2]?.ToString(),
                    Status = resultSet[row, 3]?.ToString(),
                    UpdatedAt = resultSet[row, 4] is DBNull ? DateTime.MinValue : Convert.ToDateTime(resultSet[row, 4])
                }
            )
        {
            Log.Info("[PTL Schedule Repo] Instance created (global, no station_id)");
        }

        private static string Escape(string s) => (s ?? string.Empty).Replace("'", "''");

        // ---------- CRUD ----------

        public void Insert(PickToLightSchedule entity)
        {
            var values = new object[]
            {
                entity.SequenceNo,
                entity.PayloadCsv ?? string.Empty,
                string.IsNullOrEmpty(entity.Status) ? "pending" : entity.Status
            };
            base.Insert(entity, values);
        }

        public void Update(PickToLightSchedule entity)
        {
            string q =
                "UPDATE pick_to_light_schedule SET " +
                $"sequence_no = {entity.SequenceNo}, " +
                $"payload_csv = '{Escape(entity.PayloadCsv ?? string.Empty)}', " +
                $"status = '{Escape(string.IsNullOrEmpty(entity.Status) ? "pending" : entity.Status)}' " +
                $"WHERE id = {entity.Id}";
            base.Update(entity, entity.Id, q);
        }

        public void DeleteById(int id) => base.DeleteByID(id);

        // ---------- Operational queries (GLOBAL) ----------

        /// <summary>
        /// Returns next 'pending' row globally (no station filtering).
        /// </summary>
        public PickToLightSchedule GetNextPending()
        {
            string q =
                "SELECT * FROM pick_to_light_schedule " +
                "WHERE status = 'pending' " +
                "ORDER BY sequence_no ASC, id ASC " +
                "LIMIT 1";

            var rs = ExecuteQuery(q);
            if (rs.GetLength(0) == 0) return null;
            return MapFunc(rs, 0);
        }

        /// <summary>
        /// Marks the row as 'completed'.
        /// </summary>
        public void MarkCompleted(int id)
        {
            string q =
                "UPDATE pick_to_light_schedule " +
                "SET status = 'completed' " +
                $"WHERE id = {id}";
            base.Update(null, id, q);
        }

        /// <summary>
        /// Counts all 'pending' rows globally.
        /// </summary>
        public int CountPending()
        {
            string q = "SELECT COUNT(*) FROM pick_to_light_schedule WHERE status = 'pending'";
            var rs = ExecuteQuery(q);
            if (rs.GetLength(0) == 0) return 0;
            return Convert.ToInt32(rs[0, 0]);
        }

        /// <summary>
        /// Resets all 'completed' rows globally back to 'pending'.
        /// </summary>
        public void ResetAllCompletedToPending()
        {
            string q =
                "UPDATE pick_to_light_schedule " +
                "SET status = 'pending' " +
                "WHERE status = 'completed'";
            base.Update(null, 0, q);
        }

        /// <summary>
        /// Returns the full list ordered (useful for UI/diagnostics).
        /// </summary>
        public List<PickToLightSchedule> GetAllOrdered()
        {
            string q =
                "SELECT * FROM pick_to_light_schedule " +
                "ORDER BY sequence_no ASC, id ASC";

            var rs = ExecuteQuery(q);
            var list = new List<PickToLightSchedule>();
            int rows = rs.GetLength(0);
            for (int i = 0; i < rows; i++)
                list.Add(MapFunc(rs, i));
            return list;
        }
    }
}
