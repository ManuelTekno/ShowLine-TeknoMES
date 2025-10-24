using System;

namespace NETCode.Entities
{
    public class ProductionUnitOperationResult
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public int OperationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }
}
