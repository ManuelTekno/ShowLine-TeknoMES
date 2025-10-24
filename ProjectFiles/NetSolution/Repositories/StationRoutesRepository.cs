using System;
using System.Collections.Generic;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class StationRoutesRepository : OptixRepositoryBase<StationRoute>, IOptixRepository<StationRoute>
    {
        public StationRoutesRepository()
            : base(
                "station_routes",
                new string[] { "station_id", "quality", "destination", "priority", "enabled" },
                (resultSet, row) => new StationRoute
                {
                    Id = Convert.ToInt32(resultSet[row, 0]),
                    StationId = Convert.ToInt32(resultSet[row, 1]),
                    Quality = resultSet[row, 2].ToString(),
                    Destination = Convert.ToSByte(resultSet[row, 3]),
                    Priority = Convert.ToInt32(resultSet[row, 4]),
                    Enabled = Convert.ToInt32(resultSet[row, 5]) != 0
                }
            )
        {
            Log.Info("StationRoutes Repository instance created");
        }

        // --------- CRUD ---------

        public void Insert(StationRoute entity)
        {
            var values = new object[]
            {
                entity.StationId,
                entity.Quality,
                entity.Destination,
                entity.Priority,
                entity.Enabled ? 1 : 0
            };
            base.Insert(entity, values);
        }

        public void Update(StationRoute entity)
        {
            // Note: keep style consistent with your StationRepository
            string query =
                $"UPDATE station_routes SET " +
                $"station_id = '{entity.StationId}', " +
                $"quality = '{Escape(entity.Quality)}', " +
                $"destination = '{entity.Destination}', " +
                $"priority = '{entity.Priority}', " +
                $"enabled = '{(entity.Enabled ? 1 : 0)}' " +
                $"WHERE id = {entity.Id}";

            ExecuteQuery(query);
        }

        public void DeleteById(int id)
        {
            string query = $"DELETE FROM station_routes WHERE id = {id}";
            ExecuteQuery(query);
        }

        // --------- Queries----------

        public List<StationRoute> GetEnabledRulesOrderedByPriority()
        {
            const string query =
                "SELECT * " +
                "FROM station_routes " +
                "WHERE enabled = 1 " +
                "ORDER BY priority ASC, id ASC";

            Log.Info($"[StationRoutesRepo] SQL: {query}");
            var rs = ExecuteQuery(query);
            return MapAll(rs);
        }

        public List<StationRoute> GetByStationId(int stationId, bool onlyEnabled = true)
        {
            string query =
                "SELECT * " +
                "FROM station_routes " +
                $"WHERE station_id = {stationId} {(onlyEnabled ? "AND enabled = 1 " : string.Empty)}" +
                "ORDER BY priority ASC, id ASC";
            Log.Info($"[StationRoutesRepo] SQL: {query}");

            var rs = ExecuteQuery(query);
            return MapAll(rs);
        }

        public List<StationRoute> GetAllRoutes()
        {
            const string query =
                "SELECT * " +
                "FROM station_routes " +
                "ORDER BY station_id ASC, priority ASC, id ASC";
            Log.Info($"[StationRoutesRepo] SQL: {query}");

            var rs = ExecuteQuery(query);
            return MapAll(rs);
        }

        /// <summary>
        /// Get the best (lowest priority) rule for station + quality.
        /// Falls back to 'Any' if no exact quality match exists.
        /// </summary>
        public StationRoute GetBestRule(int stationId, string quality)
        {
            // Try exact quality
            string queryExact =
                "SELECT id, station_id, quality, destination, priority, enabled " +
                "FROM station_routes " +
                $"WHERE enabled = 1 AND station_id = {stationId} AND quality = '{Escape(quality)}' " +
                "ORDER BY priority ASC, id ASC LIMIT 1";
            var rsExact = ExecuteQuery(queryExact);
            if (rsExact.GetLength(0) > 0)
                return Map(rsExact, 0);

            // Fallback to 'Any'
            const string any = "Any";
            string queryAny =
                "SELECT id, station_id, quality, destination, priority, enabled " +
                "FROM station_routes " +
                $"WHERE enabled = 1 AND station_id = {stationId} AND quality = '{any}' " +
                "ORDER BY priority ASC, id ASC LIMIT 1";
            var rsAny = ExecuteQuery(queryAny);
            if (rsAny.GetLength(0) > 0)
                return Map(rsAny, 0);

            return null;
        }

        public bool ExistsById(int id)
        {
            string query = $"SELECT 1 FROM station_routes WHERE id = {id} LIMIT 1";
            Log.Info($"[StationRoutesRepo] SQL: {query}");
            var rs = ExecuteQuery(query);
            // Si hay al menos 1 fila, existe
            return rs.GetLength(0) > 0;
        }


        // --------- Mapping helpers (no CultureInfo) ---------

        private List<StationRoute> MapAll(object[,] rs)
        {
            var list = new List<StationRoute>();
            int rows = rs.GetLength(0);
            for (int i = 0; i < rows; i++)
                list.Add(Map(rs, i));
            return list;
        }

        private StationRoute Map(object[,] rs, int row)
        {
            return new StationRoute
            {
                Id = Convert.ToInt32(rs[row, 0]),
                StationId = Convert.ToInt32(rs[row, 1]),
                Quality = rs[row, 2].ToString(),
                Destination = Convert.ToSByte(rs[row, 3]),
                Priority = Convert.ToInt32(rs[row, 4]),
                Enabled = Convert.ToInt32(rs[row, 5]) != 0
            };
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // Minimal escaping for quotes and backslashes
            return s.Replace("\\", "\\\\").Replace("'", "''");
        }
    }
}
