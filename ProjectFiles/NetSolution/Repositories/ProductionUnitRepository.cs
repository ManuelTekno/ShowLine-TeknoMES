using NETCode.Core;
using NETCode.Repositories;
using NETCode.Entities;
using System;
using UAManagedCore;

namespace NETCode.Repositories
{
    public class ProductionUnitRepository : OptixRepositoryBase<ProductionUnit>, IOptixRepository<ProductionUnit>
    {
        public ProductionUnitRepository()
            : base(
                "production_units",
                new string[] {
                    "serial_code",
                    "creation_date",
                    "unit_status",
                    "quality_status",
                    "current_station_id",
                    "pallet_id",
                    "variant_id",
                    "finished_at",
                    "is_archived"
                },
                (resultSet, row) => new ProductionUnit
                {
                    Id = Convert.ToInt32(resultSet[row, 0]),
                    SerialCode = resultSet[row, 1]?.ToString(),
                    CreationDate = Convert.ToDateTime(resultSet[row, 2]),
                    UnitStatus = resultSet[row, 3]?.ToString(),
                    QualityStatus = resultSet[row, 4]?.ToString(),
                    CurrentStationId = Convert.ToInt32(resultSet[row, 5]),
                    PalletId = Convert.ToInt32(resultSet[row, 6]),
                    VariantId = Convert.ToInt32(resultSet[row, 7]),
                    FinishedAt = resultSet[row, 8] is DBNull ? (DateTime?)null : Convert.ToDateTime(resultSet[row, 8]),
                    IsArchived = resultSet[row, 9] is DBNull ? false : Convert.ToBoolean(resultSet[row, 9])
                }
            )
        {
            Log.Info("ProductionUnit Repository instance created");
        }

        // --- Helpers simples ---
        private static string Escape(string s) => s?.Replace("'", "''");
        private static string SqlOrNull(DateTime? dt) => dt.HasValue ? $"'{dt:yyyy-MM-dd HH:mm:ss}'" : "NULL";

        public void Insert(ProductionUnit entity)
        {
            var values = new object[]
            {
                Escape(entity.SerialCode),
                entity.CreationDate ?? DateTime.Now,
                Escape(entity.UnitStatus ?? "In_Process"),
                Escape(entity.QualityStatus ?? "Pending"),
                entity.CurrentStationId,
                entity.PalletId,
                entity.VariantId,
                entity.FinishedAt,                
                entity.IsArchived
            };

            base.Insert(entity, values);
        }

        public void Update(ProductionUnit entity)
        {
            string query =
                "UPDATE production_units SET " +
                $"serial_code = '{Escape(entity.SerialCode)}', " +
                $"unit_status = '{Escape(entity.UnitStatus)}', " +
                $"quality_status = '{Escape(entity.QualityStatus)}', " +
                $"current_station_id = {entity.CurrentStationId}, " +
                $"pallet_id = {entity.PalletId}, " +
                $"variant_id = {entity.VariantId}, " +
                $"finished_at = {SqlOrNull(entity.FinishedAt)}, " +
                $"is_archived = {(entity.IsArchived ? 1 : 0)} " +
                $"WHERE id = {entity.Id}";

            ExecuteQuery(query);
        }

        private ProductionUnit ParseRow(object[,] resultSet, int row)
        {
            return new ProductionUnit
            {
                Id = Convert.ToInt32(resultSet[row, 0]),
                SerialCode = resultSet[row, 1]?.ToString(),
                CreationDate = Convert.ToDateTime(resultSet[row, 2]),
                UnitStatus = resultSet[row, 3]?.ToString(),
                QualityStatus = resultSet[row, 4]?.ToString(),
                CurrentStationId = Convert.ToInt32(resultSet[row, 5]),
                PalletId = Convert.ToInt32(resultSet[row, 6]),
                VariantId = Convert.ToInt32(resultSet[row, 7]),
                FinishedAt = resultSet[row, 8] is DBNull ? (DateTime?)null : Convert.ToDateTime(resultSet[row, 8]),
                IsArchived = resultSet[row, 9] is DBNull ? false : Convert.ToBoolean(resultSet[row, 9])
            };
        }

        public void ArchiveProductionUnit(int id)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string query = $@"
        UPDATE production_units
        SET finished_at = '{now}',
            is_archived = 1,
            unit_status = 'Completed'
        WHERE id = {id}";

            Log.Info("SQL => " + query);
            ExecuteQuery(query);
        }



        public ProductionUnit FindBySerialCode(string serialCode)
        {
            string query = $"SELECT * FROM production_units WHERE serial_code = '{Escape(serialCode)}'";
            var resultSet = ExecuteQuery(query);
            if (resultSet.GetLength(0) == 0) return null;
            return ParseRow(resultSet, 0);
        }

        public bool ExistsBySerialOrPallet(string serialCode, int palletId)
        {
            string query = $"SELECT id FROM production_units WHERE serial_code = '{Escape(serialCode)}' OR pallet_id = {palletId}";
            var resultSet = ExecuteQuery(query);
            return resultSet.GetLength(0) > 0;
        }

        public ProductionUnit FindBySerialAndPallet(string serialCode, int palletId)
        {
            string query = $"SELECT * FROM production_units WHERE serial_code = '{Escape(serialCode)}' AND pallet_id = {palletId} AND is_archived = false";
            var resultSet = ExecuteQuery(query);
            return resultSet.GetLength(0) == 0 ? null : ParseRow(resultSet, 0);
        }

        public void UpdateCurrentStation(string serialCode, int palletId, int newStationId, string newUnitStatus, string newQualityStatus = "Pending")
        {

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string query =
                "UPDATE production_units SET " +
                $"current_station_id = {newStationId}, " +
                $"unit_status = '{newUnitStatus}', " +
                $"quality_status = '{newQualityStatus}', " +
                $"finished_at = '{now}' " +   // <-- fecha actual pasada explícitamente
                $"WHERE serial_code = '{serialCode}' AND pallet_id = {palletId}";

            ExecuteQuery(query);
        }


        public ProductionUnit FindByPallet(int palletId)
        {
            string query = $"SELECT * FROM production_units WHERE pallet_id = {palletId} AND is_archived = false";
            var resultSet = ExecuteQuery(query);
            return resultSet.GetLength(0) == 0 ? null : ParseRow(resultSet, 0);
        }


        public bool ExistsActiveByPallet(int palletId, int excludeUnitId)
        {
            string q = $"SELECT id FROM production_units WHERE is_archived = 0 AND pallet_id = {palletId} AND id <> {excludeUnitId}";
            var rs = ExecuteQuery(q);
            return rs.GetLength(0) > 0;
        }

        public void UpdatePartial(int id, int? palletId = null, string unitStatus = null, string qualityStatus = null, int? currentStationId = null)
        {
            var sets = new System.Collections.Generic.List<string>();
            if (palletId.HasValue) sets.Add($"pallet_id = {palletId.Value}");
            if (!string.IsNullOrWhiteSpace(unitStatus)) sets.Add($"unit_status = '{unitStatus.Replace("'", "''")}'");
            if (!string.IsNullOrWhiteSpace(qualityStatus)) sets.Add($"quality_status = '{qualityStatus.Replace("'", "''")}'");
            if (currentStationId.HasValue) sets.Add($"current_station_id = {currentStationId.Value}");

            if (sets.Count == 0) { Log.Info($"[PURepo] No fields to update for id={id}."); return; }

            string q = $"UPDATE production_units SET {string.Join(", ", sets)} WHERE id = {id}";
            Log.Info("[PURepo] SQL => " + q);
            ExecuteQuery(q);
        }

        public int HardDeleteById(int id)
        {
            string q = $"DELETE FROM production_units WHERE id = {id}";
            Log.Info("[PURepo] SQL => " + q);
            ExecuteQuery(q);
            return 1;
        }

        public void ArchiveBySerial(string serialCode)
        {
            var u = FindBySerialCode(serialCode);
            if (u == null) throw new Exception($"Serial '{serialCode}' not found.");
            ArchiveProductionUnit(u.Id);
        }
        }
    }



