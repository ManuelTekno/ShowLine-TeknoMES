using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETCode.Core;
using NETCode.Entities;
using NETCode.Services;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class PickToLightBinRepository : OptixRepositoryBase<PickToLightBin>, IOptixRepository<PickToLightBin>
    {
        public PickToLightBinRepository()
            : base(
                // Table name
                "pick_to_light_bins",
                // Insertable columns (order matters for base.Insert)
                new string[] { "bin_position", "bin_label", "part_number", "active" },
                // Mapper: resultSet[row, colIndex] -> entity
                (resultSet, row) => new PickToLightBin
                {
                    Id = Convert.ToInt32(resultSet[row, 0]),
                    BinPosition = Convert.ToInt32(resultSet[row, 1]),
                    BinLabel = resultSet[row, 2]?.ToString(),
                    PartNumber = resultSet[row, 3]?.ToString(),
                    Active = Convert.ToInt32(resultSet[row, 4]) == 1,
                    LastUpdated = SafeParseDateTime(resultSet[row, 5])
                }
            )
        {
            Log.Info("PickToLightBinRepository instance created");
        }

        // -----------------------
        // Basic CRUD
        // -----------------------

        public void Insert(PickToLightBin entity)
        {
            var values = new object[]
            {
                entity.BinPosition,
                entity.BinLabel ?? "",
                entity.PartNumber ?? "",
                entity.Active ? 1 : 0
            };
            base.Insert(entity, values);
        }

        public void Update(PickToLightBin entity)
        {
            string query =
                $"UPDATE pick_to_light_bins " +
                $"SET bin_position = {entity.BinPosition}, " +
                $"    bin_label    = '{Escape(entity.BinLabel)}', " +
                $"    part_number  = '{Escape(entity.PartNumber)}', " +
                $"    active       = {(entity.Active ? 1 : 0)} " +
                $"WHERE id = {entity.Id}";

            Log.Info($"[PTLBinRepo] SQL: {query}");
            ExecuteQuery(query);
        }

        public bool ExistsById(int id)
        {
            string query = $"SELECT 1 FROM pick_to_light_bins WHERE id = {id} LIMIT 1";
            var rs = ExecuteQuery(query);
            return rs.GetLength(0) > 0;
        }

        // -----------------------
        // Lookups / Helpers
        // -----------------------

        public PickToLightBin GetByBinPosition(int binPosition, bool onlyActive = true)
        {
                string query = $@"
                SELECT id, bin_position, bin_label, part_number, active, last_updated
                FROM pick_to_light_bins
                WHERE bin_position = {binPosition}
                {(onlyActive ? "AND active = 1" : "")}
                ORDER BY id DESC
                LIMIT 1";


            var rs = ExecuteQuery(query);
            if (rs.GetLength(0) == 0) return null;

            return Map(rs, 0);
        }

        public List<PickToLightBin> GetActiveBins()
        {
            string query =
                "SELECT id, bin_position, bin_label, part_number, active, last_updated " +
                "FROM pick_to_light_bins " +
                "WHERE active = 1 " +
                "ORDER BY bin_position ASC";

            var rs = ExecuteQuery(query);
            return MapAll(rs);
        }

        public List<PickToLightBin> GetByPartNumber(string partNumber, bool onlyActive = true)
        {
            string query =
                "SELECT id, bin_position, bin_label, part_number, active, last_updated " +
                "FROM pick_to_light_bins " +
                $"WHERE part_number = '{Escape(partNumber)}' " +
                (onlyActive ? "AND active = 1 " : "") +
                "ORDER BY bin_position ASC";

            var rs = ExecuteQuery(query);
            return MapAll(rs);
        }

        public bool ActivateBin(int binPosition)
        {
            string query = $"UPDATE pick_to_light_bins SET active = 1 WHERE bin_position = {binPosition};";
            var rs = ExecuteQuery(query);
            return true;
        }

        public bool DeactivateBin(int binPosition)
        {
            string query = $"UPDATE pick_to_light_bins SET active = 0 WHERE bin_position = {binPosition};";
            var rs = ExecuteQuery(query);
            return true;
        }

        /// <summary>
        /// Upsert-like helper: if there is already a row for this bin_position, update it; otherwise insert a new one.
        /// Keeps it simple and aligned with your current pattern (no transactions here).
        /// </summary>
        public void SetMapping(int binPosition, string partNumber, string binLabel = null, bool active = true)
        {
            var existing = GetByBinPosition(binPosition, onlyActive: false);
            if (existing == null)
            {
                Insert(new PickToLightBin
                {
                    BinPosition = binPosition,
                    BinLabel = binLabel,
                    PartNumber = partNumber,
                    Active = active
                });
            }
            else
            {
                existing.BinLabel = binLabel;
                existing.PartNumber = partNumber;
                existing.Active = active;
                Update(existing);
            }
        }

        // -----------------------
        // Runtime: resolve bins for an operation (multi-pick)
        // -----------------------

        /// <summary>
        /// Returns distinct bin positions for the given operation, by joining operation_picks with pick_to_light_bins (active).
        /// </summary>
        public List<int> GetBinPositionsForParts(IEnumerable<string> partNumbers)
        {
            var list = (partNumbers ?? Enumerable.Empty<string>())
                       .Where(p => !string.IsNullOrWhiteSpace(p))
                       .Select(Escape) // reuse your Escape helper
                       .ToList();

            if (list.Count == 0) return new List<int>();

            string inClause = string.Join(",", list.Select(p => $"'{p}'"));

            string query =
                "SELECT bin_position " +
                "FROM pick_to_light_bins " +
                $"WHERE active = 1 AND part_number IN ({inClause}) " +
                "ORDER BY bin_position";


            var rs = ExecuteQuery(query);

            var bins = new List<int>();
            for (int i = 0; i < rs.GetLength(0); i++)
                bins.Add(Convert.ToInt32(rs[i, 0]));

            return bins;
        }

        // -----------------------
        // Mapping helpers
        // -----------------------

        private List<PickToLightBin> MapAll(object[,] rs)
        {
            var list = new List<PickToLightBin>();
            for (int i = 0; i < rs.GetLength(0); i++)
                list.Add(Map(rs, i));
            return list;
        }

        private PickToLightBin Map(object[,] rs, int row)
        {
            return new PickToLightBin
            {
                Id = Convert.ToInt32(rs[row, 0]),
                BinPosition = Convert.ToInt32(rs[row, 1]),
                BinLabel = rs[row, 2]?.ToString(),
                PartNumber = rs[row, 3]?.ToString(),
                Active = Convert.ToInt32(rs[row, 4]) == 1,
                LastUpdated = SafeParseDateTime(rs[row, 5])
            };
        }

        private static DateTime SafeParseDateTime(object value)
        {
            if (value == null) return DateTime.MinValue;
            if (value is DateTime dt) return dt;
            DateTime.TryParse(value.ToString(), out var parsed);
            return parsed;
        }

        private static string Escape(string input)
        {
            return (input ?? string.Empty).Replace("'", "''");
        }
    }
}
