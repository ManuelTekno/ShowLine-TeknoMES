using System;

namespace NETCode.Entities
{
    public class ProductionUnitResult
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public int StationId { get; set; }
        public float? CycleTime { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTime? FinishedAt { get; set; }
    }
}
