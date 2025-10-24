using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Entities;

public class ProductionUnit
{
    public int Id { get; set; }
    public string SerialCode { get; set; } = string.Empty;
    public DateTime? CreationDate { get; set; }
    public string UnitStatus { get; set; } = "In_Process";
    public string QualityStatus { get; set; } = "In_Process";
    public int CurrentStationId { get; set; }
    public int PalletId { get; set; }
    public int VariantId { get; set; }
    public DateTime? FinishedAt { get; set;}
    public bool IsArchived { get; set; }
}
